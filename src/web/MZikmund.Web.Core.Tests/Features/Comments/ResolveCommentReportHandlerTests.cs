using Microsoft.EntityFrameworkCore;
using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Web.Core.Features.Comments;
using MZikmund.Web.Core.Tests.Infrastructure;
using MZikmund.Web.Data;
using static MZikmund.Web.Core.Tests.Features.Comments.CommentSeed;
using DataCommentStatus = MZikmund.Web.Data.Entities.CommentStatus;

namespace MZikmund.Web.Core.Tests.Features.Comments;

public class ResolveCommentReportHandlerTests
{
	private readonly Guid _postId = Guid.NewGuid();
	private readonly Guid _authorId = Guid.NewGuid();
	private readonly Guid _reporterId = Guid.NewGuid();

	[Fact]
	public async Task Handle_CommentWithOpenReports_ResolvesThemAndLeavesTheCommentVisible()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		await SeedAsync(db);
		var comment = AddComment(db, _postId, _authorId, "fine actually");
		comment.ReportCount = 2;
		AddReport(db, comment.Id, _reporterId, CommentReportReason.Spam, Day(2));
		AddReport(db, comment.Id, _authorId, CommentReportReason.Other, Day(2));
		await db.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(db);

		// Act
		await handler.Handle(new ResolveCommentReportCommand(comment.Id), CancellationToken.None);

		// Assert
		Assert.Equal(0, await db.CommentReport.CountAsync(r => r.CommentId == comment.Id && r.ResolvedOn == null, CancellationToken.None));
		var stored = await db.Comment.FirstAsync(c => c.Id == comment.Id, CancellationToken.None);
		Assert.Equal(DataCommentStatus.Visible, stored.Status);
		Assert.Equal("fine actually", stored.Content);
		Assert.Equal(0, stored.ReportCount);
	}

	[Fact]
	public async Task Handle_CalledTwice_KeepsTheFirstResolvedOn()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		await SeedAsync(db);
		var comment = AddComment(db, _postId, _authorId);
		var report = AddReport(db, comment.Id, _reporterId, CommentReportReason.Spam, Day(2));
		await db.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(db);

		// Act
		await handler.Handle(new ResolveCommentReportCommand(comment.Id), CancellationToken.None);
		await handler.Handle(new ResolveCommentReportCommand(comment.Id), CancellationToken.None);

		// Assert
		var stored = await db.CommentReport.FirstAsync(r => r.Id == report.Id, CancellationToken.None);
		Assert.Equal(Now.UtcDateTime, stored.ResolvedOn);
	}

	[Fact]
	public async Task Handle_ResolvedComment_DisappearsFromTheReportQueue()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		await SeedAsync(db);
		var comment = AddComment(db, _postId, _authorId);
		AddReport(db, comment.Id, _reporterId, CommentReportReason.Spam, Day(2));
		await db.SaveChangesAsync(CancellationToken.None);

		// Act
		await CreateHandler(db).Handle(new ResolveCommentReportCommand(comment.Id), CancellationToken.None);
		var queue = await new GetCommentReportsHandler(db)
			.Handle(new GetCommentReportsQuery(1, 20), CancellationToken.None);

		// Assert
		Assert.Empty(queue.Data);
		Assert.Equal(0, queue.TotalCount);
	}

	[Fact]
	public async Task Handle_CommentWithNoOpenReports_ChangesNothing()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		await SeedAsync(db);
		var comment = AddComment(db, _postId, _authorId);
		await db.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(db);

		// Act
		await handler.Handle(new ResolveCommentReportCommand(comment.Id), CancellationToken.None);

		// Assert
		var stored = await db.Comment.FirstAsync(c => c.Id == comment.Id, CancellationToken.None);
		Assert.Equal(DataCommentStatus.Visible, stored.Status);
		Assert.Equal(0, stored.ReportCount);
	}

	[Fact]
	public async Task Handle_UnknownComment_ThrowsKeyNotFoundException()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		var handler = CreateHandler(db);

		// Act & Assert
		await Assert.ThrowsAsync<KeyNotFoundException>(
			() => handler.Handle(new ResolveCommentReportCommand(Guid.NewGuid()), CancellationToken.None));
	}

	private async Task SeedAsync(DatabaseContext db)
	{
		AddPost(db, _postId);
		AddAuthor(db, _authorId, "Commenter");
		AddAuthor(db, _reporterId, "Reporter");
		await db.SaveChangesAsync(CancellationToken.None);
	}

	private static ResolveCommentReportHandler CreateHandler(DatabaseContext db) =>
		new(db, FixedClock().Object, Logger<ResolveCommentReportHandler>().Object);
}
