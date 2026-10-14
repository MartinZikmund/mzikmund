using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MZikmund.DataContracts;
using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Web.Configuration;
using MZikmund.Web.Controllers;
using MZikmund.Web.Core.Features.Comments;
using MZikmund.Web.Core.Services.Comments;
using MZikmund.Web.Filters;
using MZikmund.Web.Infrastructure;

namespace MZikmund.Web.Tests.Controllers;

/// <summary>
/// Sends real requests through routing, authorization, the feature gate and the exception filter.
/// </summary>
public class CommentsHttpTests
{
	private static readonly Guid Id = new("22222222-2222-2222-2222-222222222222");

	[Fact]
	public async Task GetComments_Anonymous_ReturnsOk()
	{
		// Arrange
		Mock<IMediator> mediator = new();
		mediator
			.Setup(x => x.Send(It.IsAny<GetPostCommentsQuery>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PagedResponse<CommentDto>(Array.Empty<CommentDto>(), 1, 20, 0));
		using var host = await StartHostAsync(mediator, commentsEnabled: true);
		using var client = host.GetTestClient();

		// Act
		var response = await client.GetAsync($"/api/v1/posts/{Id}/comments?sort=New&pageNumber=2");

		// Assert
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		mediator.Verify(
			x => x.Send(
				It.Is<GetPostCommentsQuery>(q => q.PostId == Id && q.Sort == CommentSortOrder.New && q.PageNumber == 2),
				It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task GetComments_WhenFeatureDisabled_Returns404()
	{
		// Arrange
		using var host = await StartHostAsync(new Mock<IMediator>(), commentsEnabled: false);
		using var client = host.GetTestClient();

		// Act
		var response = await client.GetAsync($"/api/v1/posts/{Id}/comments");

		// Assert
		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}

	[Fact]
	public async Task CreateComment_Anonymous_Returns401()
	{
		// Arrange
		using var host = await StartHostAsync(new Mock<IMediator>(), commentsEnabled: true);
		using var client = host.GetTestClient();

		// Act
		var response = await client.PostAsJsonAsync($"/api/v1/posts/{Id}/comments", new { content = "Hi" });

		// Assert
		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}

	[Theory]
	[InlineData(typeof(CommentNotFoundException), HttpStatusCode.NotFound)]
	[InlineData(typeof(CommentForbiddenException), HttpStatusCode.Forbidden)]
	[InlineData(typeof(CommentConflictException), HttpStatusCode.Conflict)]
	[InlineData(typeof(CommentValidationException), HttpStatusCode.BadRequest)]
	public async Task CreateComment_WhenHandlerThrows_MapsToProblemStatus(Type exceptionType, HttpStatusCode expected)
	{
		// Arrange
		Mock<IMediator> mediator = new();
		mediator
			.Setup(x => x.Send(It.IsAny<CreateCommentCommand>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync((Exception)Activator.CreateInstance(exceptionType, "nope")!);
		using var host = await StartHostAsync(mediator, commentsEnabled: true);
		using var client = host.GetTestClient();
		client.DefaultRequestHeaders.Add(TestAuthHandler.Header, "reader");

		// Act
		var response = await client.PostAsJsonAsync($"/api/v1/posts/{Id}/comments", new { content = "Hi" });

		// Assert
		Assert.Equal(expected, response.StatusCode);
		var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
		Assert.NotEqual("nope", problem!.Detail);
		Assert.False(string.IsNullOrWhiteSpace(problem.Detail));
	}

	[Fact]
	public async Task AddVote_RoutesPutToTheVoteCommand()
	{
		// Arrange
		Mock<IMediator> mediator = new();
		mediator
			.Setup(x => x.Send(It.IsAny<SetCommentVoteCommand>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new CommentDto
			{
				Id = Id,
				PostId = Id,
				AuthorDisplayName = "A",
				Content = "c",
				ContentHtml = "<p>c</p>",
				Status = CommentStatus.Visible,
				CreatedOn = DateTime.UtcNow,
				UpvoteCount = 0,
				ReplyCount = 0,
				HasUpvoted = false,
				IsOwn = false,
			});
		using var host = await StartHostAsync(mediator, commentsEnabled: true);
		using var client = host.GetTestClient();
		client.DefaultRequestHeaders.Add(TestAuthHandler.Header, "reader");

		// Act
		var response = await client.PutAsync($"/api/v1/comments/{Id}/vote", null);

		// Assert
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		mediator.Verify(
			x => x.Send(It.Is<SetCommentVoteCommand>(c => c.CommentId == Id && c.Upvoted), It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Theory]
	[InlineData(null, HttpStatusCode.Unauthorized)]
	[InlineData("reader", HttpStatusCode.Forbidden)]
	[InlineData("admin", HttpStatusCode.OK)]
	public async Task AdminFeed_EnforcesTheAdminPolicy_EvenWhileCommentsAreDisabled(string? user, HttpStatusCode expected)
	{
		// Arrange
		Mock<IMediator> mediator = new();
		mediator
			.Setup(x => x.Send(It.IsAny<GetModerationFeedQuery>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PagedResponse<CommentModerationDto>(Array.Empty<CommentModerationDto>(), 1, 20, 0));
		using var host = await StartHostAsync(mediator, commentsEnabled: false);
		using var client = host.GetTestClient();
		if (user is not null)
		{
			client.DefaultRequestHeaders.Add(TestAuthHandler.Header, user);
		}

		// Act
		var response = await client.GetAsync("/api/v1/admin/comments");

		// Assert
		Assert.Equal(expected, response.StatusCode);
	}

	[Fact]
	public async Task BanAuthor_IsReachableAtTheAuthorsRoute()
	{
		// Arrange
		Mock<IMediator> mediator = new();
		using var host = await StartHostAsync(mediator, commentsEnabled: false);
		using var client = host.GetTestClient();
		client.DefaultRequestHeaders.Add(TestAuthHandler.Header, "admin");

		// Act
		var response = await client.PostAsync($"/api/v1/admin/authors/{Id}/ban", null);

		// Assert
		Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
		mediator.Verify(x => x.Send(It.Is<BanAuthorCommand>(c => c.AuthorId == Id), It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task GetModerationFeed_WithZuluSince_PassesUtcToTheQuery()
	{
		// Arrange
		Mock<IMediator> mediator = new();
		mediator
			.Setup(x => x.Send(It.IsAny<GetModerationFeedQuery>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PagedResponse<CommentModerationDto>(Array.Empty<CommentModerationDto>(), 1, 20, 0));
		using var host = await StartHostAsync(mediator, commentsEnabled: false);
		using var client = host.GetTestClient();
		client.DefaultRequestHeaders.Add(TestAuthHandler.Header, "admin");

		// Act
		await client.GetAsync("/api/v1/admin/comments?since=2026-08-01T10:00:00Z");

		// Assert
		mediator.Verify(
			x => x.Send(
				It.Is<GetModerationFeedQuery>(q =>
					q.Since == new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc)
					&& q.Since!.Value.Kind == DateTimeKind.Utc),
				It.IsAny<CancellationToken>()),
			Times.Once);
	}

	private static async Task<IHost> StartHostAsync(Mock<IMediator> mediator, bool commentsEnabled) =>
		await new HostBuilder()
			.ConfigureWebHost(web => web
				.UseTestServer()
				.ConfigureServices(services =>
				{
					services.AddLogging();
					services.AddSingleton(mediator.Object);
					services.AddSingleton(new Mock<ITurnstileVerifier>().Object);
					services.AddSingleton<IOptions<CommentsOptions>>(
						Options.Create(new CommentsOptions { Enabled = commentsEnabled }));
					services.AddScoped<CommentsEnabledFilter>();
					services.AddCommentRateLimiting(new RateLimitOptions());
					services.AddControllers()
						.AddApplicationPart(typeof(CommentsController).Assembly)
						.AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
					services.AddAuthentication(TestAuthHandler.Scheme)
						.AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.Scheme, _ => { });
					services.AddAuthorization(options => options.AddPolicy(
						"AdminPolicy",
						policy => policy.RequireClaim("role", "admin")));
				})
				.Configure(app =>
				{
					app.UseRouting();
					app.UseAuthentication();
					app.UseRateLimiter();
					app.UseAuthorization();
					app.UseEndpoints(endpoints => endpoints.MapControllers());
				}))
			.StartAsync();

	private sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
	{
		public const string Scheme = "Test";
		public const string Header = "X-Test-User";

		public TestAuthHandler(
			IOptionsMonitor<AuthenticationSchemeOptions> options,
			ILoggerFactory logger,
			UrlEncoder encoder)
			: base(options, logger, encoder)
		{
		}

		protected override Task<AuthenticateResult> HandleAuthenticateAsync()
		{
			if (!Request.Headers.TryGetValue(Header, out var user))
			{
				return Task.FromResult(AuthenticateResult.NoResult());
			}

			List<Claim> claims = new() { new Claim("sub", Guid.NewGuid().ToString()) };
			if (user == "admin")
			{
				claims.Add(new Claim("role", "admin"));
			}

			ClaimsIdentity identity = new(claims, Scheme);
			return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme)));
		}
	}
}
