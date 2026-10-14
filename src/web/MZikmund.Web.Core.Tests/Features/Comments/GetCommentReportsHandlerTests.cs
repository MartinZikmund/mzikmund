using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Web.Core.Features.Comments;
using MZikmund.Web.Core.Tests.Infrastructure;
using MZikmund.Web.Data;
using static MZikmund.Web.Core.Tests.Features.Comments.CommentSeed;
using DataCommentStatus = MZikmund.Web.Data.Entities.CommentStatus;

namespace MZikmund.Web.Core.Tests.Features.Comments;

public class GetCommentReportsHandlerTests
{
	private readonly Guid _postId = Guid.NewGuid();
	private readonly Guid _authorId = Guid.NewGuid();
	private readonly Guid _reporterId = Guid.NewGuid();

	[Fact]
	public async Task Handle_TenReportsOnOneComment_ReturnsASingleRow()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		await SeedAsync(db);
		var comment = AddComment(db, _postId, _authorId);
		for (var i = 0; i < 10; i++)
		{
			AddReport(db, comment.Id, NewReporter(db), CommentReportReason.Spam, Day(2).AddMinutes(i));
		}

		await db.SaveChangesAsync(CancellationToken.None);
		var handler = new GetCommentReportsHandler(db);

		// Act
		var result = await handler.Handle(new GetCommentReportsQuery(1, 20), CancellationToken.None);

		// Assert
		var row = Assert.Single(result.Data);
		Assert.Equal(comment.Id, row.CommentId);
		Assert.Equal(10, row.OpenReportCount);
		Assert.Equal(1, result.TotalCount);
		Assert.Equal(Day(2), row.FirstReportedOn);
	}

	[Fact]
	public async Task Handle_MixedReasons_ReturnsEachDistinctReasonOnce()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		await SeedAsync(db);
		var comment = AddComment(db, _postId, _authorId);
		AddReport(db, comment.Id, NewReporter(db), CommentReportReason.Spam, Day(2));
		AddReport(db, comment.Id, NewReporter(db), CommentReportReason.Spam, Day(2));
		AddReport(db, comment.Id, NewReporter(db), CommentReportReason.Abuse, Day(2));
		await db.SaveChangesAsync(CancellationToken.None);
		var handler = new GetCommentReportsHandler(db);

		// Act
		var result = await handler.Handle(new GetCommentReportsQuery(1, 20), CancellationToken.None);

		// Assert
		var row = Assert.Single(result.Data);
		Assert.Equal(3, row.OpenReportCount);
		Assert.Equal(new[] { CommentReportReason.Spam, CommentReportReason.Abuse }, row.Reasons);
	}

	[Fact]
	public async Task Handle_ResolvedReports_AreExcluded()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		await SeedAsync(db);
		var open = AddComment(db, _postId, _authorId);
		var resolved = AddComment(db, _postId, _authorId);
		AddReport(db, open.Id, _reporterId, CommentReportReason.Spam, Day(2));
		AddReport(db, resolved.Id, _reporterId, CommentReportReason.Abuse, Day(2)).ResolvedOn = Day(3);
		await db.SaveChangesAsync(CancellationToken.None);
		var handler = new GetCommentReportsHandler(db);

		// Act
		var result = await handler.Handle(new GetCommentReportsQuery(1, 20), CancellationToken.None);

		// Assert
		var row = Assert.Single(result.Data);
		Assert.Equal(open.Id, row.CommentId);
		Assert.Equal(1, result.TotalCount);
	}

	[Fact]
	public async Task Handle_SeveralComments_AreOrderedOldestReportFirstAndCarryTheAuthorId()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		await SeedAsync(db);
		var older = AddComment(db, _postId, _authorId, "older body");
		var newer = AddComment(db, _postId, _authorId);
		AddReport(db, newer.Id, _reporterId, CommentReportReason.Spam, Day(3));
		AddReport(db, older.Id, _reporterId, CommentReportReason.Spam, Day(2));
		await db.SaveChangesAsync(CancellationToken.None);
		var handler = new GetCommentReportsHandler(db);

		// Act
		var result = await handler.Handle(new GetCommentReportsQuery(1, 20), CancellationToken.None);

		// Assert
		var rows = result.Data.ToArray();
		Assert.Equal(new[] { older.Id, newer.Id }, rows.Select(r => r.CommentId));
		Assert.All(rows, r => Assert.Equal(_authorId, r.AuthorId));
		Assert.Equal("older body", rows[0].Comment.Content);
		Assert.Equal("Commenter", rows[0].Comment.AuthorDisplayName);
		Assert.False(rows[0].Comment.HasUpvoted);
		Assert.False(rows[0].Comment.IsOwn);
		Assert.Empty(rows[0].Comment.Replies);
	}

	[Fact]
	public async Task Handle_SecondPage_SkipsTheFirstAndKeepsTheTotal()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		await SeedAsync(db);
		var first = AddComment(db, _postId, _authorId);
		var second = AddComment(db, _postId, _authorId);
		AddReport(db, first.Id, _reporterId, CommentReportReason.Spam, Day(2));
		AddReport(db, second.Id, _reporterId, CommentReportReason.Spam, Day(3));
		await db.SaveChangesAsync(CancellationToken.None);
		var handler = new GetCommentReportsHandler(db);

		// Act
		var result = await handler.Handle(new GetCommentReportsQuery(2, 1), CancellationToken.None);

		// Assert
		Assert.Equal(second.Id, Assert.Single(result.Data).CommentId);
		Assert.Equal(2, result.TotalCount);
	}

	[Fact]
	public async Task Handle_HiddenComment_StaysBlankedInTheQueue()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		await SeedAsync(db);
		var comment = AddComment(db, _postId, _authorId, "secret", status: DataCommentStatus.HiddenByModerator);
		AddReport(db, comment.Id, _reporterId, CommentReportReason.Spam, Day(2));
		await db.SaveChangesAsync(CancellationToken.None);
		var handler = new GetCommentReportsHandler(db);

		// Act
		var result = await handler.Handle(new GetCommentReportsQuery(1, 20), CancellationToken.None);

		// Assert
		var row = Assert.Single(result.Data);
		Assert.Equal(MZikmund.DataContracts.Blog.Comments.CommentStatus.HiddenByModerator, row.Comment.Status);
		Assert.Equal("", row.Comment.Content);
	}

	[Fact]
	public async Task Handle_EmptyQueue_ReturnsAnEmptyPage()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		var handler = new GetCommentReportsHandler(db);

		// Act
		var result = await handler.Handle(new GetCommentReportsQuery(1, 20), CancellationToken.None);

		// Assert
		Assert.Empty(result.Data);
		Assert.Equal(0, result.TotalCount);
		Assert.Equal(1, result.PageNumber);
		Assert.Equal(20, result.PageSize);
	}

	[Theory]
	[InlineData(0, 20)]
	[InlineData(1, 0)]
	public async Task Handle_InvalidPaging_ThrowsArgumentOutOfRangeException(int pageNumber, int pageSize)
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		var handler = new GetCommentReportsHandler(db);

		// Act & Assert
		await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
			() => handler.Handle(new GetCommentReportsQuery(pageNumber, pageSize), CancellationToken.None));
	}

	private static Guid NewReporter(DatabaseContext db)
	{
		var id = Guid.NewGuid();
		AddAuthor(db, id, $"Reporter{id:N}");
		return id;
	}

	private async Task SeedAsync(DatabaseContext db)
	{
		AddPost(db, _postId);
		AddAuthor(db, _authorId, "Commenter");
		AddAuthor(db, _reporterId, "Reporter");
		await db.SaveChangesAsync(CancellationToken.None);
	}
}
