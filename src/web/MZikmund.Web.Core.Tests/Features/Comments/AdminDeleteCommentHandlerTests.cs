using Microsoft.EntityFrameworkCore;
using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Web.Core.Features.Comments;
using MZikmund.Web.Core.Services.Comments;
using MZikmund.Web.Core.Tests.Infrastructure;
using MZikmund.Web.Data;
using static MZikmund.Web.Core.Tests.Features.Comments.CommentSeed;
using DataCommentStatus = MZikmund.Web.Data.Entities.CommentStatus;

namespace MZikmund.Web.Core.Tests.Features.Comments;

public class AdminDeleteCommentHandlerTests
{
	private readonly Guid _postId = Guid.NewGuid();
	private readonly Guid _authorId = Guid.NewGuid();
	private readonly Guid _reporterId = Guid.NewGuid();

	[Fact]
	public async Task Handle_VisibleComment_PurgesTheTextAndMarksItHiddenByModerator()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		await SeedAsync(db);
		var comment = AddComment(db, _postId, _authorId, "unacceptable");
		await db.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(db);

		// Act
		await handler.Handle(new AdminDeleteCommentCommand(comment.Id), CancellationToken.None);

		// Assert
		var stored = await db.Comment.FirstAsync(c => c.Id == comment.Id, CancellationToken.None);
		Assert.Equal(DataCommentStatus.HiddenByModerator, stored.Status);
		Assert.Equal("", stored.Content);
		Assert.Equal("", stored.ContentHtml);
	}

	[Fact]
	public async Task Handle_CommentWithReplies_KeepsTheRowAndTheRepliesAttached()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		await SeedAsync(db);
		var parent = AddComment(db, _postId, _authorId, replyCount: 1);
		var reply = AddComment(db, _postId, _reporterId, parentId: parent.Id);
		await db.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(db);

		// Act
		await handler.Handle(new AdminDeleteCommentCommand(parent.Id), CancellationToken.None);

		// Assert
		Assert.Equal(2, await db.Comment.CountAsync(c => c.PostId == _postId, CancellationToken.None));
		var storedReply = await db.Comment.FirstAsync(c => c.Id == reply.Id, CancellationToken.None);
		Assert.Equal(parent.Id, storedReply.ParentId);
		Assert.Equal(DataCommentStatus.Visible, storedReply.Status);
		var storedParent = await db.Comment.FirstAsync(c => c.Id == parent.Id, CancellationToken.None);
		Assert.Equal(1, storedParent.ReplyCount);
	}

	[Fact]
	public async Task Handle_CommentWithOpenReports_ResolvesThemAndZeroesReportCount()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		await SeedAsync(db);
		var comment = AddComment(db, _postId, _authorId);
		comment.ReportCount = 2;
		AddReport(db, comment.Id, _reporterId, CommentReportReason.Spam, Day(2));
		AddReport(db, comment.Id, _authorId, CommentReportReason.Other, Day(3));
		await db.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(db);

		// Act
		await handler.Handle(new AdminDeleteCommentCommand(comment.Id), CancellationToken.None);

		// Assert
		Assert.Equal(0, await db.CommentReport.CountAsync(r => r.CommentId == comment.Id && r.ResolvedOn == null, CancellationToken.None));
		var stored = await db.Comment.FirstAsync(c => c.Id == comment.Id, CancellationToken.None);
		Assert.Equal(0, stored.ReportCount);
	}

	[Fact]
	public async Task Handle_VisibleReply_DecrementsTheParentReplyCount()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		await SeedAsync(db);
		var parent = AddComment(db, _postId, _authorId, replyCount: 1);
		var reply = AddComment(db, _postId, _reporterId, parentId: parent.Id);
		await db.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(db);

		// Act
		await handler.Handle(new AdminDeleteCommentCommand(reply.Id), CancellationToken.None);

		// Assert
		var stored = await db.Comment.FirstAsync(c => c.Id == parent.Id, CancellationToken.None);
		Assert.Equal(0, stored.ReplyCount);
	}

	[Fact]
	public async Task Handle_HiddenReply_PurgesTheTextWithoutTouchingTheParentReplyCount()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		await SeedAsync(db);
		var parent = AddComment(db, _postId, _authorId, replyCount: 1);
		var reply = AddComment(db, _postId, _reporterId, "hidden words", parentId: parent.Id, status: DataCommentStatus.HiddenByModerator);
		await db.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(db);

		// Act
		await handler.Handle(new AdminDeleteCommentCommand(reply.Id), CancellationToken.None);

		// Assert
		var storedReply = await db.Comment.FirstAsync(c => c.Id == reply.Id, CancellationToken.None);
		Assert.Equal("", storedReply.Content);
		var storedParent = await db.Comment.FirstAsync(c => c.Id == parent.Id, CancellationToken.None);
		Assert.Equal(1, storedParent.ReplyCount);
	}

	[Fact]
	public async Task Handle_UnknownComment_ThrowsCommentNotFoundException()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		var handler = CreateHandler(db);

		// Act & Assert
		await Assert.ThrowsAsync<CommentNotFoundException>(
			() => handler.Handle(new AdminDeleteCommentCommand(Guid.NewGuid()), CancellationToken.None));
	}

	private async Task SeedAsync(DatabaseContext db)
	{
		AddPost(db, _postId);
		AddAuthor(db, _authorId, "Commenter");
		AddAuthor(db, _reporterId, "Reporter");
		await db.SaveChangesAsync(CancellationToken.None);
	}

	private static AdminDeleteCommentHandler CreateHandler(DatabaseContext db) =>
		new(db, FixedClock().Object, Logger<AdminDeleteCommentHandler>().Object);
}
