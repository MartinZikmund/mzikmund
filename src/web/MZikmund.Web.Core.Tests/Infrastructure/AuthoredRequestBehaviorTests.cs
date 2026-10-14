using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using MZikmund.Web.Core.Infrastructure;
using MZikmund.Web.Core.Services;
using MZikmund.Web.Core.Services.Comments;
using MZikmund.Web.Data;
using MZikmund.Web.Data.Entities;

namespace MZikmund.Web.Core.Tests.Infrastructure;

public class AuthoredRequestBehaviorTests : IDisposable
{
	private static readonly DateTimeOffset Now = new(2026, 8, 11, 12, 0, 0, TimeSpan.Zero);
	private static readonly Guid AuthorId = new("11111111-1111-1111-1111-111111111111");

	private readonly CommentsDbFixture _fixture = new();
	private readonly Mock<ICurrentUserAccessor> _mockCurrentUserAccessor = new();
	private readonly Mock<IDateProvider> _mockDateProvider = new();

	public AuthoredRequestBehaviorTests()
	{
		_mockDateProvider.Setup(d => d.UtcNow).Returns(Now);
	}

	public void Dispose()
	{
		_fixture.Dispose();
		GC.SuppressFinalize(this);
	}

	private AuthoredRequestBehavior<TestAuthoredRequest, string> CreateBehavior(DatabaseContext context) =>
		new(
			context,
			_mockCurrentUserAccessor.Object,
			_mockDateProvider.Object,
			new Mock<ILogger<AuthoredRequestBehavior<TestAuthoredRequest, string>>>().Object);

	private async Task SeedAuthorAsync(bool isBanned, string displayName = "Old Name")
	{
		using var context = _fixture.CreateContext();
		context.CommentAuthor.Add(new CommentAuthorEntity
		{
			Id = AuthorId,
			DisplayName = displayName,
			AvatarUrl = null,
			Email = "old@example.com",
			IsBanned = isBanned,
			NotifyOnReply = true,
			CreatedOn = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
			LastSeenOn = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
		});
		await context.SaveChangesAsync(CancellationToken.None);
	}

	[Fact]
	public async Task Handle_WhenAuthorDoesNotExist_InsertsAuthorAndCallsNext()
	{
		// Arrange
		_mockCurrentUserAccessor
			.Setup(a => a.Current)
			.Returns(new CommentAuthorInfo(AuthorId, "Martin", "https://cdn/a.png", "martin@example.com", false));

		var nextCalled = false;
		using var context = _fixture.CreateContext();
		var behavior = CreateBehavior(context);

		// Act
		var result = await behavior.Handle(
			new TestAuthoredRequest("payload"),
			_ =>
			{
				nextCalled = true;
				return Task.FromResult("handled");
			},
			CancellationToken.None);

		// Assert
		Assert.True(nextCalled);
		Assert.Equal("handled", result);

		using var assertContext = _fixture.CreateContext();
		var author = await assertContext.CommentAuthor.SingleAsync(a => a.Id == AuthorId, CancellationToken.None);
		Assert.Equal("Martin", author.DisplayName);
		Assert.Equal("https://cdn/a.png", author.AvatarUrl);
		Assert.Equal("martin@example.com", author.Email);
		Assert.False(author.IsBanned);
		Assert.Equal(Now.UtcDateTime, author.CreatedOn);
		Assert.Equal(Now.UtcDateTime, author.LastSeenOn);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task Handle_PersistsTheAdminFlagFromTheCaller(bool isAdmin)
	{
		// Arrange
		await SeedAuthorAsync(isBanned: false);
		_mockCurrentUserAccessor
			.Setup(a => a.Current)
			.Returns(new CommentAuthorInfo(AuthorId, "Martin", null, "martin@example.com", isAdmin));

		using var context = _fixture.CreateContext();
		var behavior = CreateBehavior(context);

		// Act
		await behavior.Handle(new TestAuthoredRequest("payload"), _ => Task.FromResult("handled"), CancellationToken.None);

		// Assert
		using var assertContext = _fixture.CreateContext();
		var author = await assertContext.CommentAuthor.SingleAsync(a => a.Id == AuthorId, CancellationToken.None);
		Assert.Equal(isAdmin, author.IsAdmin);
	}

	[Fact]
	public async Task Handle_WhenAuthorExists_RefreshesProfileAndLastSeen()
	{
		// Arrange
		await SeedAuthorAsync(isBanned: false);
		_mockCurrentUserAccessor
			.Setup(a => a.Current)
			.Returns(new CommentAuthorInfo(AuthorId, "New Name", "https://cdn/b.png", "new@example.com", false));

		using var context = _fixture.CreateContext();
		var behavior = CreateBehavior(context);

		// Act
		await behavior.Handle(
			new TestAuthoredRequest("payload"),
			_ => Task.FromResult("handled"),
			CancellationToken.None);

		// Assert
		using var assertContext = _fixture.CreateContext();
		var author = await assertContext.CommentAuthor.SingleAsync(a => a.Id == AuthorId, CancellationToken.None);
		Assert.Equal("New Name", author.DisplayName);
		Assert.Equal("https://cdn/b.png", author.AvatarUrl);
		Assert.Equal("new@example.com", author.Email);
		Assert.Equal(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), author.CreatedOn);
		Assert.Equal(Now.UtcDateTime, author.LastSeenOn);
	}

	[Fact]
	public async Task Handle_WhenAuthorIsBanned_ThrowsAndDoesNotCallNext()
	{
		// Arrange
		await SeedAuthorAsync(isBanned: true, displayName: "Banned Person");
		_mockCurrentUserAccessor
			.Setup(a => a.Current)
			.Returns(new CommentAuthorInfo(AuthorId, "Banned Person", null, "banned@example.com", false));

		var nextCalled = false;
		using var context = _fixture.CreateContext();
		var behavior = CreateBehavior(context);

		// Act
		var exception = await Assert.ThrowsAsync<CommentForbiddenException>(() =>
			behavior.Handle(
				new TestAuthoredRequest("payload"),
				_ =>
				{
					nextCalled = true;
					return Task.FromResult("handled");
				},
				CancellationToken.None));

		// Assert
		Assert.False(nextCalled);
		Assert.Contains("banned", exception.Message, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public async Task Handle_WhenThereIsNoCurrentUser_ThrowsAndDoesNotCallNext()
	{
		// Arrange
		_mockCurrentUserAccessor.Setup(a => a.Current).Returns((CommentAuthorInfo?)null);

		var nextCalled = false;
		using var context = _fixture.CreateContext();
		var behavior = CreateBehavior(context);

		// Act
		await Assert.ThrowsAsync<CommentForbiddenException>(() =>
			behavior.Handle(
				new TestAuthoredRequest("payload"),
				_ =>
				{
					nextCalled = true;
					return Task.FromResult("handled");
				},
				CancellationToken.None));

		// Assert
		Assert.False(nextCalled);
		Assert.Equal(0, await context.CommentAuthor.CountAsync(CancellationToken.None));
	}

	[Fact]
	public void OpenBehaviorRegistration_AppliesOnlyToAuthoredRequests()
	{
		// Arrange
		ServiceCollection services = new();
		services.AddScoped<ICurrentUserAccessor>(_ => _mockCurrentUserAccessor.Object);
		services.AddScoped<IDateProvider>(_ => _mockDateProvider.Object);
		services.AddScoped(_ => _fixture.CreateContext());
		services.AddLogging();
		services.AddMediatR(config =>
		{
			config.RegisterServicesFromAssemblyContaining<AuthoredRequestBehaviorTests>();
			config.AddOpenBehavior(typeof(AuthoredRequestBehavior<,>));
		});
		using var provider = services.BuildServiceProvider();
		using var scope = provider.CreateScope();

		// Act
		var authored = scope.ServiceProvider.GetServices<IPipelineBehavior<TestAuthoredRequest, string>>();
		var unmarked = scope.ServiceProvider.GetServices<IPipelineBehavior<TestUnmarkedRequest, string>>();

		// Assert
		Assert.Single(authored);
		Assert.Empty(unmarked);
	}

	public record TestAuthoredRequest(string Value) : IRequest<string>, IAuthoredRequest;

	public record TestUnmarkedRequest(string Value) : IRequest<string>;
}
