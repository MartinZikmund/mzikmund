using Microsoft.EntityFrameworkCore;
using Moq;
using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Web.Core.Features.Comments;
using MZikmund.Web.Core.Services.Comments;
using MZikmund.Web.Core.Tests.Infrastructure;
using MZikmund.Web.Data;
using static MZikmund.Web.Core.Tests.Features.Comments.CommentSeed;

namespace MZikmund.Web.Core.Tests.Features.Comments;

public class ReportCommentHandlerTests
{
	private readonly Guid _postId = Guid.NewGuid();
	private readonly Guid _reporterId = Guid.NewGuid();
	private readonly Guid _secondReporterId = Guid.NewGuid();
	private readonly Guid _commentAuthorId = Guid.NewGuid();

	[Fact]
	public async Task Handle_FirstReport_CreatesReportAndIncrementsReportCount()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		var commentId = await SeedCommentAsync(db);
		var handler = CreateHandler(db, CurrentUser(_reporterId, "Martin"));

		// Act
		await handler.Handle(new ReportCommentCommand(commentId, CommentReportReason.Spam, "  buy my stuff  "), CancellationToken.None);

		// Assert
		var report = await db.CommentReport.FirstAsync(r => r.CommentId == commentId, CancellationToken.None);
		Assert.Equal(_reporterId, report.ReporterId);
		Assert.Equal(CommentReportReason.Spam, report.Reason);
		Assert.Equal("buy my stuff", report.Note);
		Assert.Null(report.ResolvedOn);
		Assert.Equal(Now.UtcDateTime, report.CreatedOn);

		var stored = await db.Comment.FirstAsync(c => c.Id == commentId, CancellationToken.None);
		Assert.Equal(1, stored.ReportCount);
	}

	[Fact]
	public async Task Handle_DuplicateOpenReportFromSameReporter_IsSuppressed()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		var commentId = await SeedCommentAsync(db);
		var handler = CreateHandler(db, CurrentUser(_reporterId, "Martin"));

		// Act
		await handler.Handle(new ReportCommentCommand(commentId, CommentReportReason.Spam, null), CancellationToken.None);
		await handler.Handle(new ReportCommentCommand(commentId, CommentReportReason.Abuse, null), CancellationToken.None);

		// Assert
		Assert.Equal(1, await db.CommentReport.CountAsync(r => r.CommentId == commentId, CancellationToken.None));
		var stored = await db.Comment.FirstAsync(c => c.Id == commentId, CancellationToken.None);
		Assert.Equal(1, stored.ReportCount);
	}

	[Fact]
	public async Task Handle_ReportAgainAfterResolution_IsAccepted()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		var commentId = await SeedCommentAsync(db);
		var handler = CreateHandler(db, CurrentUser(_reporterId, "Martin"));
		await handler.Handle(new ReportCommentCommand(commentId, CommentReportReason.Spam, null), CancellationToken.None);
		var first = await db.CommentReport.FirstAsync(r => r.CommentId == commentId, CancellationToken.None);
		first.ResolvedOn = Now.UtcDateTime;
		await db.SaveChangesAsync(CancellationToken.None);

		// Act
		await handler.Handle(new ReportCommentCommand(commentId, CommentReportReason.Spam, null), CancellationToken.None);

		// Assert
		Assert.Equal(2, await db.CommentReport.CountAsync(r => r.CommentId == commentId, CancellationToken.None));
	}

	[Fact]
	public async Task Handle_ReportFromDifferentReporter_IncrementsReportCountAgain()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		var commentId = await SeedCommentAsync(db);
		await CreateHandler(db, CurrentUser(_reporterId, "Martin"))
			.Handle(new ReportCommentCommand(commentId, CommentReportReason.Spam, null), CancellationToken.None);
		var secondHandler = CreateHandler(db, CurrentUser(_secondReporterId, "Alex"));

		// Act
		await secondHandler.Handle(new ReportCommentCommand(commentId, CommentReportReason.Abuse, null), CancellationToken.None);

		// Assert
		Assert.Equal(2, await db.CommentReport.CountAsync(r => r.CommentId == commentId, CancellationToken.None));
		var stored = await db.Comment.FirstAsync(c => c.Id == commentId, CancellationToken.None);
		Assert.Equal(2, stored.ReportCount);
	}

	[Fact]
	public async Task Handle_NoteOverMaxLength_ThrowsAndStoresNothing()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		var commentId = await SeedCommentAsync(db);
		var handler = CreateHandler(db, CurrentUser(_reporterId, "Martin"));
		var tooLongNote = new string('x', CommentLimits.MaxReportNoteLength + 1);

		// Act & Assert
		await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
			() => handler.Handle(new ReportCommentCommand(commentId, CommentReportReason.Other, tooLongNote), CancellationToken.None));
		Assert.Equal(0, await db.CommentReport.CountAsync(CancellationToken.None));
		var stored = await db.Comment.FirstAsync(c => c.Id == commentId, CancellationToken.None);
		Assert.Equal(0, stored.ReportCount);
	}

	[Fact]
	public async Task Handle_UnknownComment_ThrowsKeyNotFound()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		var handler = CreateHandler(db, CurrentUser(_reporterId, "Martin"));

		// Act & Assert
		await Assert.ThrowsAsync<KeyNotFoundException>(
			() => handler.Handle(new ReportCommentCommand(Guid.NewGuid(), CommentReportReason.Spam, null), CancellationToken.None));
	}

	private async Task<Guid> SeedCommentAsync(DatabaseContext db)
	{
		AddPost(db, _postId);
		AddAuthor(db, _reporterId, "Martin");
		AddAuthor(db, _secondReporterId, "Alex");
		AddAuthor(db, _commentAuthorId, "Jane");
		var comment = AddComment(db, _postId, _commentAuthorId);
		await db.SaveChangesAsync(CancellationToken.None);
		return comment.Id;
	}

	private static ReportCommentHandler CreateHandler(DatabaseContext dbContext, Mock<ICurrentUserAccessor> currentUser) =>
		new(dbContext, currentUser.Object, FixedClock().Object, Logger<ReportCommentHandler>().Object);
}
