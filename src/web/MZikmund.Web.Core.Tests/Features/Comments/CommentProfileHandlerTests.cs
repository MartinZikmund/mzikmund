using Microsoft.EntityFrameworkCore;
using Moq;
using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Web.Core.Features.Comments;
using MZikmund.Web.Core.Services.Comments;
using MZikmund.Web.Core.Tests.Infrastructure;
using MZikmund.Web.Data;
using static MZikmund.Web.Core.Tests.Features.Comments.CommentSeed;

namespace MZikmund.Web.Core.Tests.Features.Comments;

public class CommentProfileHandlerTests
{
	private readonly Guid _authorId = Guid.NewGuid();

	private Mock<ICurrentUserAccessor> User(string tokenName = "Token Name", bool isAdmin = false)
	{
		Mock<ICurrentUserAccessor> accessor = new();
		accessor
			.Setup(a => a.Current)
			.Returns(new CommentAuthorInfo(_authorId, tokenName, "https://cdn/a.png", "reader@example.com", isAdmin));
		return accessor;
	}

	private GetMyCommentProfileHandler GetHandler(DatabaseContext db, Mock<ICurrentUserAccessor> user) =>
		new(db, user.Object);

	private UpdateMyCommentProfileHandler UpdateHandler(DatabaseContext db, Mock<ICurrentUserAccessor> user) =>
		new(db, user.Object, FixedClock().Object, Logger<UpdateMyCommentProfileHandler>().Object);

	[Fact]
	public async Task Get_WithoutAnAuthorRow_ReturnsTheTokenNameAndCreatesNothing()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();

		// Act
		var profile = await GetHandler(db, User()).Handle(new GetMyCommentProfileQuery(), CancellationToken.None);

		// Assert
		Assert.Equal("Token Name", profile.DisplayName);
		Assert.False(profile.HasChosenDisplayName);
		Assert.False(profile.IsBanned);
		Assert.Empty(await db.CommentAuthor.ToListAsync(CancellationToken.None));
	}

	[Fact]
	public async Task Get_WithAChosenName_ReturnsTheStoredNameAndBanState()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		var author = AddAuthor(db, _authorId, "Stored");
		author.HasChosenDisplayName = true;
		author.IsBanned = true;
		await db.SaveChangesAsync(CancellationToken.None);

		// Act
		var profile = await GetHandler(db, User()).Handle(new GetMyCommentProfileQuery(), CancellationToken.None);

		// Assert
		Assert.Equal("Stored", profile.DisplayName);
		Assert.True(profile.HasChosenDisplayName);
		Assert.True(profile.IsBanned);
	}

	[Fact]
	public async Task Get_WithAnUnchosenName_ReturnsTheCurrentTokenName()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		AddAuthor(db, _authorId, "Old Token");
		await db.SaveChangesAsync(CancellationToken.None);

		// Act
		var profile = await GetHandler(db, User("New Token")).Handle(new GetMyCommentProfileQuery(), CancellationToken.None);

		// Assert
		Assert.Equal("New Token", profile.DisplayName);
		Assert.False(profile.HasChosenDisplayName);
	}

	[Fact]
	public async Task Update_WithoutAnAuthorRow_CreatesItWithTheChosenName()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();

		// Act
		var profile = await UpdateHandler(db, User())
			.Handle(new UpdateMyCommentProfileCommand("  Jane   Doe "), CancellationToken.None);

		// Assert
		Assert.Equal("Jane Doe", profile.DisplayName);
		Assert.True(profile.HasChosenDisplayName);
		using var assertDb = fixture.CreateContext();
		var stored = await assertDb.CommentAuthor.SingleAsync(CancellationToken.None);
		Assert.Equal(_authorId, stored.Id);
		Assert.Equal("Jane Doe", stored.DisplayName);
		Assert.True(stored.HasChosenDisplayName);
		Assert.Equal("reader@example.com", stored.Email);
		Assert.Equal("https://cdn/a.png", stored.AvatarUrl);
	}

	[Fact]
	public async Task Update_WithAnExistingRow_ChangesTheNameOfAllTheirComments()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		var postId = Guid.NewGuid();
		AddPost(db, postId);
		AddAuthor(db, _authorId, "Old");
		AddComment(db, postId, _authorId);
		await db.SaveChangesAsync(CancellationToken.None);

		// Act
		await UpdateHandler(db, User()).Handle(new UpdateMyCommentProfileCommand("Brand New"), CancellationToken.None);

		// Assert
		using var assertDb = fixture.CreateContext();
		var comment = await assertDb.Comment.Include(c => c.Author).SingleAsync(CancellationToken.None);
		Assert.Equal("Brand New", comment.Author.DisplayName);
	}

	[Theory]
	[InlineData("x", "at least")]
	[InlineData("!!!", "letter or digit")]
	public async Task Update_InvalidName_ThrowsValidationWithTheReason(string name, string detailPart)
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();

		// Act
		var ex = await Assert.ThrowsAsync<CommentValidationException>(
			() => UpdateHandler(db, User()).Handle(new UpdateMyCommentProfileCommand(name), CancellationToken.None));

		// Assert
		Assert.Equal(CommentErrorCodes.DisplayNameInvalid, ex.Code);
		Assert.Contains(detailPart, ex.PublicDetail);
		Assert.Empty(await db.CommentAuthor.ToListAsync(CancellationToken.None));
	}

	[Fact]
	public async Task Update_TooLongName_ThrowsValidation()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();

		// Act
		var ex = await Assert.ThrowsAsync<CommentValidationException>(
			() => UpdateHandler(db, User()).Handle(new UpdateMyCommentProfileCommand(new string('a', 41)), CancellationToken.None));

		// Assert
		Assert.Equal(CommentErrorCodes.DisplayNameInvalid, ex.Code);
		Assert.Contains("40", ex.PublicDetail);
	}

	[Fact]
	public async Task Update_ReservedName_IsRejectedForReadersButAllowedForAdmins()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();

		// Act
		var ex = await Assert.ThrowsAsync<CommentValidationException>(
			() => UpdateHandler(db, User()).Handle(new UpdateMyCommentProfileCommand("Martin Zikmund"), CancellationToken.None));
		var profile = await UpdateHandler(db, User(isAdmin: true))
			.Handle(new UpdateMyCommentProfileCommand("Martin Zikmund"), CancellationToken.None);

		// Assert
		Assert.Equal(CommentErrorCodes.DisplayNameReserved, ex.Code);
		Assert.Equal("Martin Zikmund", profile.DisplayName);
	}

	[Fact]
	public async Task Update_BannedAuthor_IsForbiddenAndKeepsTheName()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		AddAuthor(db, _authorId, "Old").IsBanned = true;
		await db.SaveChangesAsync(CancellationToken.None);

		// Act
		var ex = await Assert.ThrowsAsync<CommentForbiddenException>(
			() => UpdateHandler(db, User()).Handle(new UpdateMyCommentProfileCommand("Jane Doe"), CancellationToken.None));

		// Assert
		Assert.Equal(CommentErrorCodes.Banned, ex.Code);
		using var assertDb = fixture.CreateContext();
		Assert.Equal("Old", (await assertDb.CommentAuthor.SingleAsync(CancellationToken.None)).DisplayName);
	}
}
