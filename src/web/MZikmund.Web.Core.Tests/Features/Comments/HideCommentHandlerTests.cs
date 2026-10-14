using Microsoft.EntityFrameworkCore;
using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Web.Core.Features.Comments;
using MZikmund.Web.Core.Services.Comments;
using MZikmund.Web.Core.Tests.Infrastructure;
using MZikmund.Web.Data;
using static MZikmund.Web.Core.Tests.Features.Comments.CommentSeed;
using DataCommentStatus = MZikmund.Web.Data.Entities.CommentStatus;

namespace MZikmund.Web.Core.Tests.Features.Comments;

public class HideCommentHandlerTests
{
	private readonly Guid _postId = Guid.NewGuid();
	private readonly Guid _authorId = Guid.NewGuid();
	private readonly Guid _reporterId = Guid.NewGuid();

	[Fact]
	public async Task Handle_VisibleComment_HidesItButKeepsTheTextSoItCanBeReversed()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		await SeedAsync(db);
		var comment = AddComment(db, _postId, _authorId, "borderline remark");
		await db.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(db);

		// Act
		await handler.Handle(new HideCommentCommand(comment.Id), CancellationToken.None);

		// Assert
		var stored = await db.Comment.FirstAsync(c => c.Id == comment.Id, CancellationToken.None);
		Assert.Equal(DataCommentStatus.HiddenByModerator, stored.Status);
		Assert.Equal("borderline remark", stored.Content);
		Assert.Equal("<p>borderline remark</p>", stored.ContentHtml);
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
		AddReport(db, comment.Id, _authorId, CommentReportReason.Abuse, Day(3));
		await db.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(db);

		// Act
		await handler.Handle(new HideCommentCommand(comment.Id), CancellationToken.None);

		// Assert
		var reports = await db.CommentReport.Where(r => r.CommentId == comment.Id).ToListAsync(CancellationToken.None);
		Assert.Equal(2, reports.Count);
		Assert.All(reports, r => Assert.Equal(Now.UtcDateTime, r.ResolvedOn));
		var stored = await db.Comment.FirstAsync(c => c.Id == comment.Id, CancellationToken.None);
		Assert.Equal(0, stored.ReportCount);
	}

	[Fact]
	public async Task Handle_AlreadyResolvedReport_KeepsItsOriginalResolvedOn()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		await SeedAsync(db);
		var comment = AddComment(db, _postId, _authorId);
		var report = AddReport(db, comment.Id, _reporterId, CommentReportReason.OffTopic, Day(2));
		report.ResolvedOn = Day(3);
		await db.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(db);

		// Act
		await handler.Handle(new HideCommentCommand(comment.Id), CancellationToken.None);

		// Assert
		var stored = await db.CommentReport.FirstAsync(r => r.Id == report.Id, CancellationToken.None);
		Assert.Equal(Day(3), stored.ResolvedOn);
	}

	[Fact]
	public async Task Handle_OpenReportsOnAnotherComment_AreLeftUntouched()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		await SeedAsync(db);
		var hidden = AddComment(db, _postId, _authorId);
		var other = AddComment(db, _postId, _authorId);
		other.ReportCount = 1;
		var report = AddReport(db, other.Id, _reporterId, CommentReportReason.Other, Day(2));
		await db.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(db);

		// Act
		await handler.Handle(new HideCommentCommand(hidden.Id), CancellationToken.None);

		// Assert
		var storedReport = await db.CommentReport.FirstAsync(r => r.Id == report.Id, CancellationToken.None);
		Assert.Null(storedReport.ResolvedOn);
		var storedOther = await db.Comment.FirstAsync(c => c.Id == other.Id, CancellationToken.None);
		Assert.Equal(1, storedOther.ReportCount);
	}

	[Fact]
	public async Task Handle_VisibleReply_DecrementsTheParentReplyCount()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		await SeedAsync(db);
		var parent = AddComment(db, _postId, _authorId, replyCount: 2);
		var reply = AddComment(db, _postId, _reporterId, parentId: parent.Id);
		await db.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(db);

		// Act
		await handler.Handle(new HideCommentCommand(reply.Id), CancellationToken.None);

		// Assert
		var stored = await db.Comment.FirstAsync(c => c.Id == parent.Id, CancellationToken.None);
		Assert.Equal(1, stored.ReplyCount);
	}

	[Fact]
	public async Task Handle_HidingTwice_DecrementsTheParentReplyCountOnlyOnce()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		await SeedAsync(db);
		var parent = AddComment(db, _postId, _authorId, replyCount: 2);
		var reply = AddComment(db, _postId, _reporterId, parentId: parent.Id);
		await db.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(db);

		// Act
		await handler.Handle(new HideCommentCommand(reply.Id), CancellationToken.None);
		await handler.Handle(new HideCommentCommand(reply.Id), CancellationToken.None);

		// Assert
		var stored = await db.Comment.FirstAsync(c => c.Id == parent.Id, CancellationToken.None);
		Assert.Equal(1, stored.ReplyCount);
	}

	[Fact]
	public async Task Handle_ReplyDeletedByItsAuthor_LeavesTheParentReplyCountAlone()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		await SeedAsync(db);
		var parent = AddComment(db, _postId, _authorId, replyCount: 1);
		var reply = AddComment(db, _postId, _reporterId, parentId: parent.Id, status: DataCommentStatus.DeletedByAuthor);
		await db.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(db);

		// Act
		await handler.Handle(new HideCommentCommand(reply.Id), CancellationToken.None);

		// Assert
		var stored = await db.Comment.FirstAsync(c => c.Id == parent.Id, CancellationToken.None);
		Assert.Equal(1, stored.ReplyCount);
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
			() => handler.Handle(new HideCommentCommand(Guid.NewGuid()), CancellationToken.None));
	}

	private async Task SeedAsync(DatabaseContext db)
	{
		AddPost(db, _postId);
		AddAuthor(db, _authorId, "Commenter");
		AddAuthor(db, _reporterId, "Reporter");
		await db.SaveChangesAsync(CancellationToken.None);
	}

	private static HideCommentHandler CreateHandler(DatabaseContext db) =>
		new(db, FixedClock().Object, Logger<HideCommentHandler>().Object);
}
