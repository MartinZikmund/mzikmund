using Microsoft.EntityFrameworkCore;
using Moq;
using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Web.Core.Features.Comments;
using MZikmund.Web.Core.Services.Comments;
using MZikmund.Web.Core.Tests.Infrastructure;
using MZikmund.Web.Data;
using MZikmund.Web.Data.Entities;
using static MZikmund.Web.Core.Tests.Features.Comments.CommentSeed;
using DataCommentStatus = MZikmund.Web.Data.Entities.CommentStatus;

namespace MZikmund.Web.Core.Tests.Features.Comments;

public class CreateCommentHandlerTests
{
	private readonly Mock<ICommentContentRenderer> _mockRenderer;
	private readonly Guid _postId = Guid.NewGuid();
	private readonly Guid _authorId = Guid.NewGuid();
	private readonly Guid _otherAuthorId = Guid.NewGuid();

	public CreateCommentHandlerTests()
	{
		_mockRenderer = new Mock<ICommentContentRenderer>();
		_mockRenderer
			.Setup(r => r.RenderHtml(It.IsAny<string>()))
			.Returns<string>(markdown => "<p>" + markdown + "</p>");
	}

	[Fact]
	public async Task Handle_TopLevelComment_StoresRawContentAndRenderedHtml()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		await SeedAsync(db);
		var handler = CreateHandler(db);

		// Act
		var result = await handler.Handle(new CreateCommentCommand(_postId, "  **hello**  ", null), CancellationToken.None);

		// Assert
		var stored = await db.Comment.FirstAsync(c => c.Id == result.Id, CancellationToken.None);
		Assert.Equal("**hello**", stored.Content);
		Assert.Equal("<p>**hello**</p>", stored.ContentHtml);
		Assert.Null(stored.ParentId);
		Assert.Null(stored.MentionedDisplayName);
		Assert.Equal(DataCommentStatus.Visible, stored.Status);
		Assert.Equal(Now.UtcDateTime, stored.CreatedOn);
		Assert.Null(stored.EditedOn);
		Assert.True(result.IsOwn);
		Assert.Equal("Martin", result.AuthorDisplayName);
	}

	[Fact]
	public async Task Handle_ReplyToTopLevelComment_KeepsParentAndIncrementsReplyCount()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		await SeedAsync(db);
		var parent = AddComment(db, _postId, _otherAuthorId);
		await db.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(db);

		// Act
		var result = await handler.Handle(new CreateCommentCommand(_postId, "A reply", parent.Id), CancellationToken.None);

		// Assert
		var stored = await db.Comment.FirstAsync(c => c.Id == result.Id, CancellationToken.None);
		Assert.Equal(parent.Id, stored.ParentId);
		Assert.Null(stored.MentionedDisplayName);
		var reloadedParent = await db.Comment.FirstAsync(c => c.Id == parent.Id, CancellationToken.None);
		Assert.Equal(1, reloadedParent.ReplyCount);
	}

	[Fact]
	public async Task Handle_ReplyToAReply_IsRepointedToGrandparentWithMention()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		await SeedAsync(db);
		var grandParent = AddComment(db, _postId, _otherAuthorId, replyCount: 1);
		var reply = AddComment(db, _postId, _otherAuthorId, parentId: grandParent.Id);
		await db.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(db);

		// Act
		var result = await handler.Handle(new CreateCommentCommand(_postId, "Sure thing", reply.Id), CancellationToken.None);

		// Assert
		var stored = await db.Comment.FirstAsync(c => c.Id == result.Id, CancellationToken.None);
		Assert.Equal(grandParent.Id, stored.ParentId);
		Assert.Equal("Jane", stored.MentionedDisplayName);
		Assert.Equal("Sure thing", stored.Content);
		Assert.DoesNotContain("@Jane", stored.Content);

		var reloadedGrandParent = await db.Comment.FirstAsync(c => c.Id == grandParent.Id, CancellationToken.None);
		Assert.Equal(2, reloadedGrandParent.ReplyCount);
		var reloadedReply = await db.Comment.FirstAsync(c => c.Id == reply.Id, CancellationToken.None);
		Assert.Equal(0, reloadedReply.ReplyCount);
	}

	[Fact]
	public async Task Handle_ContentOverMaxLength_ThrowsAndStoresNothing()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		await SeedAsync(db);
		var handler = CreateHandler(db);
		var tooLong = new string('a', CommentLimits.MaxCommentLength + 1);

		// Act & Assert
		await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
			() => handler.Handle(new CreateCommentCommand(_postId, tooLong, null), CancellationToken.None));
		Assert.Equal(0, await db.Comment.CountAsync(CancellationToken.None));
	}

	[Fact]
	public async Task Handle_ContentExactlyAtMaxLength_IsAccepted()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		await SeedAsync(db);
		var handler = CreateHandler(db);
		var atLimit = new string('a', CommentLimits.MaxCommentLength);

		// Act
		var result = await handler.Handle(new CreateCommentCommand(_postId, atLimit, null), CancellationToken.None);

		// Assert
		var stored = await db.Comment.FirstAsync(c => c.Id == result.Id, CancellationToken.None);
		Assert.Equal(CommentLimits.MaxCommentLength, stored.Content.Length);
	}

	[Fact]
	public async Task Handle_WhitespaceOnlyContent_Throws()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		await SeedAsync(db);
		var handler = CreateHandler(db);

		// Act & Assert
		await Assert.ThrowsAsync<ArgumentException>(
			() => handler.Handle(new CreateCommentCommand(_postId, "   ", null), CancellationToken.None));
	}

	[Fact]
	public async Task Handle_OnDraftPost_ThrowsKeyNotFoundAndStoresNothing()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		var draftPostId = Guid.NewGuid();
		AddPost(db, draftPostId, PostStatus.Draft);
		AddAuthor(db, _authorId, "Martin");
		await db.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(db);

		// Act & Assert
		await Assert.ThrowsAsync<KeyNotFoundException>(
			() => handler.Handle(new CreateCommentCommand(draftPostId, "hello", null), CancellationToken.None));
		Assert.Equal(0, await db.Comment.CountAsync(CancellationToken.None));
	}

	[Fact]
	public async Task Handle_OnFutureDatedPost_ThrowsKeyNotFoundAndStoresNothing()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		var futurePostId = Guid.NewGuid();
		AddPost(db, futurePostId, publishedDate: DateTimeOffset.UtcNow.AddDays(30));
		AddAuthor(db, _authorId, "Martin");
		await db.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(db);

		// Act & Assert
		await Assert.ThrowsAsync<KeyNotFoundException>(
			() => handler.Handle(new CreateCommentCommand(futurePostId, "hello", null), CancellationToken.None));
		Assert.Equal(0, await db.Comment.CountAsync(CancellationToken.None));
	}

	[Theory]
	[InlineData(DataCommentStatus.HiddenByModerator)]
	[InlineData(DataCommentStatus.DeletedByAuthor)]
	public async Task Handle_ReplyToNonVisibleComment_ThrowsInvalidOperationAndKeepsReplyCount(DataCommentStatus status)
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		await SeedAsync(db);
		var target = AddComment(db, _postId, _otherAuthorId, status: status);
		await db.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(db);

		// Act & Assert
		await Assert.ThrowsAsync<InvalidOperationException>(
			() => handler.Handle(new CreateCommentCommand(_postId, "hello", target.Id), CancellationToken.None));
		var reloaded = await db.Comment.FirstAsync(c => c.Id == target.Id, CancellationToken.None);
		Assert.Equal(0, reloaded.ReplyCount);
		Assert.Equal(1, await db.Comment.CountAsync(CancellationToken.None));
	}

	[Fact]
	public async Task Handle_ReplyToCommentOnAnotherPost_ThrowsInvalidOperation()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		await SeedAsync(db);
		var otherPostId = Guid.NewGuid();
		AddPost(db, otherPostId);
		var target = AddComment(db, otherPostId, _otherAuthorId);
		await db.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(db);

		// Act & Assert
		await Assert.ThrowsAsync<InvalidOperationException>(
			() => handler.Handle(new CreateCommentCommand(_postId, "hello", target.Id), CancellationToken.None));
	}

	private async Task SeedAsync(DatabaseContext db)
	{
		AddPost(db, _postId);
		AddAuthor(db, _authorId, "Martin");
		AddAuthor(db, _otherAuthorId, "Jane");
		await db.SaveChangesAsync(CancellationToken.None);
	}

	private CreateCommentHandler CreateHandler(DatabaseContext dbContext) =>
		new(
			dbContext,
			CurrentUser(_authorId, "Martin").Object,
			_mockRenderer.Object,
			FixedClock().Object,
			Logger<CreateCommentHandler>().Object);
}
