using Microsoft.EntityFrameworkCore;
using MZikmund.Web.Core.Features.Comments;
using MZikmund.Web.Core.Services.Comments;
using MZikmund.Web.Core.Tests.Infrastructure;
using MZikmund.Web.Data;
using static MZikmund.Web.Core.Tests.Features.Comments.CommentSeed;
using DataCommentStatus = MZikmund.Web.Data.Entities.CommentStatus;

namespace MZikmund.Web.Core.Tests.Features.Comments;

public class DeleteCommentHandlerTests
{
	private readonly Guid _postId = Guid.NewGuid();
	private readonly Guid _authorId = Guid.NewGuid();
	private readonly Guid _otherAuthorId = Guid.NewGuid();

	[Fact]
	public async Task Handle_OwnTopLevelComment_BecomesTombstoneAndKeepsItsReplies()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		await SeedAsync(db);
		var parent = AddComment(db, _postId, _authorId, replyCount: 1);
		var reply = AddComment(db, _postId, _otherAuthorId, parentId: parent.Id);
		await db.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(db);

		// Act
		await handler.Handle(new DeleteCommentCommand(parent.Id), CancellationToken.None);

		// Assert
		var stored = await db.Comment.FirstAsync(c => c.Id == parent.Id, CancellationToken.None);
		Assert.Equal(DataCommentStatus.DeletedByAuthor, stored.Status);
		Assert.Equal(1, stored.ReplyCount);

		var storedReply = await db.Comment.FirstAsync(c => c.Id == reply.Id, CancellationToken.None);
		Assert.Equal(DataCommentStatus.Visible, storedReply.Status);
		Assert.Equal(parent.Id, storedReply.ParentId);
	}

	[Fact]
	public async Task Handle_OwnComment_BlanksTheStoredText()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		await SeedAsync(db);
		var comment = AddComment(db, _postId, _authorId, "regrettable words");
		await db.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(db);

		// Act
		await handler.Handle(new DeleteCommentCommand(comment.Id), CancellationToken.None);

		// Assert
		var stored = await db.Comment.FirstAsync(c => c.Id == comment.Id, CancellationToken.None);
		Assert.Equal("", stored.Content);
		Assert.Equal("", stored.ContentHtml);
	}

	[Fact]
	public async Task Handle_OwnReply_DecrementsParentReplyCount()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		await SeedAsync(db);
		var parent = AddComment(db, _postId, _otherAuthorId, replyCount: 1);
		var reply = AddComment(db, _postId, _authorId, parentId: parent.Id);
		await db.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(db);

		// Act
		await handler.Handle(new DeleteCommentCommand(reply.Id), CancellationToken.None);

		// Assert
		var storedParent = await db.Comment.FirstAsync(c => c.Id == parent.Id, CancellationToken.None);
		Assert.Equal(0, storedParent.ReplyCount);
		var storedReply = await db.Comment.FirstAsync(c => c.Id == reply.Id, CancellationToken.None);
		Assert.Equal(DataCommentStatus.DeletedByAuthor, storedReply.Status);
	}

	[Fact]
	public async Task Handle_AlreadyDeletedReply_DoesNotDecrementParentAgain()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		await SeedAsync(db);
		var parent = AddComment(db, _postId, _otherAuthorId, replyCount: 1);
		var reply = AddComment(db, _postId, _authorId, parentId: parent.Id);
		await db.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(db);

		// Act
		await handler.Handle(new DeleteCommentCommand(reply.Id), CancellationToken.None);
		await handler.Handle(new DeleteCommentCommand(reply.Id), CancellationToken.None);

		// Assert
		var storedParent = await db.Comment.FirstAsync(c => c.Id == parent.Id, CancellationToken.None);
		Assert.Equal(0, storedParent.ReplyCount);
	}

	[Fact]
	public async Task Handle_CommentOwnedByAnotherAuthor_Throws()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		await SeedAsync(db);
		var comment = AddComment(db, _postId, _otherAuthorId);
		await db.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(db);

		// Act & Assert
		await Assert.ThrowsAsync<CommentForbiddenException>(
			() => handler.Handle(new DeleteCommentCommand(comment.Id), CancellationToken.None));
		var stored = await db.Comment.FirstAsync(c => c.Id == comment.Id, CancellationToken.None);
		Assert.Equal(DataCommentStatus.Visible, stored.Status);
		Assert.Equal("Existing comment", stored.Content);
	}

	[Fact]
	public async Task Handle_CommentHiddenByModerator_ThrowsAndStaysHidden()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		await SeedAsync(db);
		var comment = AddComment(db, _postId, _authorId, status: DataCommentStatus.HiddenByModerator);
		await db.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(db);

		// Act & Assert
		await Assert.ThrowsAsync<CommentConflictException>(
			() => handler.Handle(new DeleteCommentCommand(comment.Id), CancellationToken.None));
		var stored = await db.Comment.FirstAsync(c => c.Id == comment.Id, CancellationToken.None);
		Assert.Equal(DataCommentStatus.HiddenByModerator, stored.Status);
	}

	[Fact]
	public async Task Handle_ModeratorHidesTheReplyBetweenReadAndWrite_ParentIsDecrementedOnlyOnce()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var seedDb = fixture.CreateContext();
		await SeedAsync(seedDb);
		var parent = AddComment(seedDb, _postId, _otherAuthorId, replyCount: 1);
		var reply = AddComment(seedDb, _postId, _authorId, parentId: parent.Id);
		await seedDb.SaveChangesAsync(CancellationToken.None);

		using var moderator = fixture.CreateContext();
		await moderator.Comment.AnyAsync(CancellationToken.None);
		RaceInterceptor race = new("FROM \"Comment\"", async () =>
		{
			var hide = new HideCommentHandler(moderator, FixedClock().Object, Logger<HideCommentHandler>().Object);
			await hide.Handle(new HideCommentCommand(reply.Id), CancellationToken.None);
		});
		using var db = fixture.CreateContext(race);
		var handler = CreateHandler(db);

		// Act
		await handler.Handle(new DeleteCommentCommand(reply.Id), CancellationToken.None);

		// Assert
		Assert.True(race.Fired);
		using var assertDb = fixture.CreateContext();
		var storedParent = await assertDb.Comment.FirstAsync(c => c.Id == parent.Id, CancellationToken.None);
		Assert.Equal(0, storedParent.ReplyCount);
		var storedReply = await assertDb.Comment.FirstAsync(c => c.Id == reply.Id, CancellationToken.None);
		Assert.Equal(DataCommentStatus.HiddenByModerator, storedReply.Status);
	}

	private async Task SeedAsync(DatabaseContext db)
	{
		AddPost(db, _postId);
		AddAuthor(db, _authorId, "Martin");
		AddAuthor(db, _otherAuthorId, "Jane");
		await db.SaveChangesAsync(CancellationToken.None);
	}

	private DeleteCommentHandler CreateHandler(DatabaseContext dbContext) =>
		new(dbContext, CurrentUser(_authorId, "Martin").Object, Logger<DeleteCommentHandler>().Object);
}
