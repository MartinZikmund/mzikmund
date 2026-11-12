using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Web.Configuration;
using MZikmund.Web.Controllers;
using MZikmund.Web.Core.Properties;
using MZikmund.Web.Core.Infrastructure;
using MZikmund.Web.Core.Services;
using MZikmund.Web.Core.Services.Comments;
using MZikmund.Web.Data;
using MZikmund.Web.Data.Entities;
using MZikmund.Web.Filters;
using MZikmund.Web.Infrastructure;
using MZikmund.Web.Infrastructure.Auth;
using MZikmund.Web.Services;

namespace MZikmund.Web.Tests.Controllers;

/// <summary>
/// Runs the comment profile endpoints through the real pipeline: auth, feature gate, rate limiter,
/// MediatR with the author behavior, and a SQLite database.
/// </summary>
public sealed class CommentProfileHttpTests : IDisposable
{
	private static readonly Guid UserId = new("33333333-3333-3333-3333-333333333333");

	private readonly SqliteConnection _connection = new("Filename=:memory:");

	public CommentProfileHttpTests()
	{
		_connection.Open();
		using var db = CreateDb();
		db.Database.EnsureCreated();
	}

	public void Dispose() => _connection.Dispose();

	[Fact]
	public async Task GetProfile_Anonymous_Returns401()
	{
		// Arrange
		using var host = await StartHostAsync();
		using var client = host.GetTestClient();

		// Act
		var response = await client.GetAsync("/api/v1/comments/me");

		// Assert
		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}

	[Fact]
	public async Task UpdateProfile_Anonymous_Returns401()
	{
		// Arrange
		using var host = await StartHostAsync();
		using var client = host.GetTestClient();

		// Act
		var response = await client.PutAsJsonAsync("/api/v1/comments/me", new { displayName = "Jane Doe" });

		// Assert
		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}

	[Fact]
	public async Task Profile_WhenCommentsAreDisabled_Returns404()
	{
		// Arrange
		using var host = await StartHostAsync(commentsEnabled: false);
		using var client = CreateClient(host);

		// Act
		var get = await client.GetAsync("/api/v1/comments/me");
		var put = await client.PutAsJsonAsync("/api/v1/comments/me", new { displayName = "Jane Doe" });

		// Assert
		Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
		Assert.Equal(HttpStatusCode.NotFound, put.StatusCode);
	}

	[Fact]
	public async Task GetProfile_EmailUserWithoutAName_GetsAReaderNameAndNoRow()
	{
		// Arrange
		using var host = await StartHostAsync();
		using var client = CreateClient(host, email: "martin@zikmund.dev");

		// Act
		var response = await client.GetAsync("/api/v1/comments/me");

		// Assert
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
		Assert.Matches("^Reader [0-9A-F]{4}$", document.RootElement.GetProperty("displayName").GetString());
		Assert.False(document.RootElement.GetProperty("hasChosenDisplayName").GetBoolean());
		Assert.False(document.RootElement.GetProperty("isBanned").GetBoolean());
		using var db = CreateDb();
		Assert.Empty(await db.CommentAuthor.ToListAsync());
	}

	[Fact]
	public async Task UpdateProfile_ValidName_IsStoredAndReturnedByGet()
	{
		// Arrange
		using var host = await StartHostAsync();
		using var client = CreateClient(host);

		// Act
		var put = await client.PutAsJsonAsync("/api/v1/comments/me", new { displayName = "  Jane   Doe " });
		var get = await client.GetFromJsonAsync<CommentProfile>("/api/v1/comments/me");

		// Assert
		Assert.Equal(HttpStatusCode.OK, put.StatusCode);
		var updated = await put.Content.ReadFromJsonAsync<CommentProfile>();
		Assert.Equal("Jane Doe", updated!.DisplayName);
		Assert.True(updated.HasChosenDisplayName);
		Assert.Equal("Jane Doe", get!.DisplayName);
		Assert.True(get.HasChosenDisplayName);
	}

	[Theory]
	[InlineData("x", "display_name_invalid")]
	[InlineData("!!", "display_name_invalid")]
	[InlineData("Martin Zikmund", "display_name_reserved")]
	[InlineData("Admin", "display_name_reserved")]
	public async Task UpdateProfile_RejectedName_Returns400WithTheCode(string name, string code)
	{
		// Arrange
		using var host = await StartHostAsync();
		using var client = CreateClient(host);

		// Act
		var response = await client.PutAsJsonAsync("/api/v1/comments/me", new { displayName = name });

		// Assert
		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
		using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
		Assert.Equal(code, document.RootElement.GetProperty("code").GetString());
		Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("detail").GetString()));
	}

	[Fact]
	public async Task UpdateProfile_InvalidName_DetailExplainsWhy()
	{
		// Arrange
		using var host = await StartHostAsync();
		using var client = CreateClient(host);

		// Act
		var response = await client.PutAsJsonAsync("/api/v1/comments/me", new { displayName = "x" });

		// Assert
		var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
		Assert.Contains("at least 2", problem!.Detail);
	}

	[Fact]
	public async Task UpdateProfile_BannedAuthor_Returns403WithTheBannedCode()
	{
		// Arrange
		using var host = await StartHostAsync();
		using (var db = CreateDb())
		{
			db.CommentAuthor.Add(new CommentAuthorEntity
			{
				Id = UserId,
				DisplayName = "Old",
				Email = "old@example.com",
				IsBanned = true,
				CreatedOn = DateTime.UtcNow,
				LastSeenOn = DateTime.UtcNow,
			});
			await db.SaveChangesAsync();
		}

		using var client = CreateClient(host);

		// Act
		var response = await client.PutAsJsonAsync("/api/v1/comments/me", new { displayName = "Jane Doe" });

		// Assert
		Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
		using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
		Assert.Equal(CommentErrorCodes.Banned, document.RootElement.GetProperty("code").GetString());
	}

	[Fact]
	public async Task UpdateProfile_PastTheEditLimit_Returns429()
	{
		// Arrange
		RateLimitOptions limits = new() { CommentEdit = new() { PermitLimit = 1, WindowMinutes = 10 } };
		using var host = await StartHostAsync(limits: limits);
		using var client = CreateClient(host);

		// Act
		var first = await client.PutAsJsonAsync("/api/v1/comments/me", new { displayName = "Jane Doe" });
		var second = await client.PutAsJsonAsync("/api/v1/comments/me", new { displayName = "Jane Roe" });

		// Assert
		Assert.Equal(HttpStatusCode.OK, first.StatusCode);
		Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode);
	}

	[Fact]
	public async Task Comment_FromAnEmailUserWithoutAName_NeverShowsTheEmailPrefix()
	{
		// Arrange
		var postId = await SeedPostAsync();
		using var host = await StartHostAsync();
		using var client = CreateClient(host, email: "secretlocalpart@example.com");
		client.DefaultRequestHeaders.Add(CommentsController.ClientHeaderName, CommentsController.AppClientHeaderValue);

		// Act
		var created = await client.PostAsJsonAsync($"/api/v1/posts/{postId}/comments", new { content = "Hello there" });
		using var anonymous = host.GetTestClient();
		var listed = await anonymous.GetStringAsync($"/api/v1/posts/{postId}/comments");

		// Assert
		Assert.Equal(HttpStatusCode.OK, created.StatusCode);
		Assert.DoesNotContain("secretlocalpart", listed, StringComparison.OrdinalIgnoreCase);
		Assert.Matches("\"authorDisplayName\":\"Reader [0-9A-F]{4}\"", listed);
	}

	[Fact]
	public async Task Comment_AfterRenaming_ShowsTheChosenNameEvenWhenTheTokenSaysOtherwise()
	{
		// Arrange
		var postId = await SeedPostAsync();
		using var host = await StartHostAsync();
		using var client = CreateClient(host, tokenName: "Token Name");
		client.DefaultRequestHeaders.Add(CommentsController.ClientHeaderName, CommentsController.AppClientHeaderValue);

		// Act
		await client.PutAsJsonAsync("/api/v1/comments/me", new { displayName = "Chosen Name" });
		await client.PostAsJsonAsync($"/api/v1/posts/{postId}/comments", new { content = "Hello there" });
		using var anonymous = host.GetTestClient();
		var listed = await anonymous.GetStringAsync($"/api/v1/posts/{postId}/comments");

		// Assert
		Assert.Contains("\"authorDisplayName\":\"Chosen Name\"", listed);
	}

	private async Task<Guid> SeedPostAsync()
	{
		var postId = Guid.NewGuid();
		using var db = CreateDb();
		db.Post.Add(new PostEntity
		{
			Id = postId,
			Title = "Post",
			RouteName = "post",
			LanguageCode = "en-US",
			Content = "content",
			Abstract = "abstract",
			Status = PostStatus.Published,
			CreatedDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
			PublishedDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
		});
		await db.SaveChangesAsync();
		return postId;
	}

	private DatabaseContext CreateDb() =>
		new(new DbContextOptionsBuilder<DatabaseContext>().UseLazyLoadingProxies().UseSqlite(_connection).Options);

	private static HttpClient CreateClient(IHost host, string email = "reader@example.com", string? tokenName = null)
	{
		var client = host.GetTestClient();
		client.DefaultRequestHeaders.Add(TestAuthHandler.EmailHeader, email);
		if (tokenName is not null)
		{
			client.DefaultRequestHeaders.Add(TestAuthHandler.NameHeader, tokenName);
		}

		client.DefaultRequestHeaders.Add(TestAuthHandler.Header, "reader");
		return client;
	}

	private async Task<IHost> StartHostAsync(bool commentsEnabled = true, RateLimitOptions? limits = null) =>
		await new HostBuilder()
			.ConfigureWebHost(web => web
				.UseTestServer()
				.ConfigureServices(services =>
				{
					services.AddLogging();
					services.AddHttpContextAccessor();
					services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
					services.AddSingleton<IDateProvider, DateProvider>();
					services.AddSingleton<ICommentContentRenderer, CommentContentRenderer>();
					services.AddSingleton(new Mock<ITurnstileVerifier>().Object);
					services.AddDbContext<DatabaseContext>(options => options.UseLazyLoadingProxies().UseSqlite(_connection));
					services.AddMediatR(config =>
					{
						config.RegisterServicesFromAssemblyContaining<CoreAssemblyMarker>();
						config.AddOpenBehavior(typeof(AuthoredRequestBehavior<,>));
					});
					services.AddSingleton<IOptions<CommentsOptions>>(
						Options.Create(new CommentsOptions { Enabled = commentsEnabled }));
					services.AddScoped<CommentsEnabledFilter>();
					services.AddCommentRateLimiting(limits ?? new RateLimitOptions());
					services.AddControllers().AddApplicationPart(typeof(CommentsController).Assembly);
					services.AddAuthentication(TestAuthHandler.Scheme)
						.AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.Scheme, _ => { });
					services.AddAuthorization();
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
		public const string EmailHeader = "X-Test-Email";
		public const string NameHeader = "X-Test-Name";

		public TestAuthHandler(
			IOptionsMonitor<AuthenticationSchemeOptions> options,
			ILoggerFactory logger,
			UrlEncoder encoder)
			: base(options, logger, encoder)
		{
		}

		protected override Task<AuthenticateResult> HandleAuthenticateAsync()
		{
			if (!Request.Headers.ContainsKey(Header))
			{
				return Task.FromResult(AuthenticateResult.NoResult());
			}

			List<Claim> claims =
			[
				new Claim(SupabaseClaims.Subject, UserId.ToString()),
				new Claim(SupabaseClaims.Email, Request.Headers[EmailHeader].ToString()),
				new Claim(SupabaseClaims.Projected, "true"),
			];

			if (Request.Headers.TryGetValue(NameHeader, out var name))
			{
				claims.Add(new Claim(SupabaseClaims.DisplayName, name.ToString()));
			}

			ClaimsIdentity identity = new(claims, Scheme);
			return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme)));
		}
	}
}
