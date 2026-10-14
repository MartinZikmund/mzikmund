using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Web.Core.Features.Comments;
using MZikmund.Web.Core.Tests.Infrastructure;
using MZikmund.Web.Data;
using static MZikmund.Web.Core.Tests.Features.Comments.CommentSeed;
using DataCommentStatus = MZikmund.Web.Data.Entities.CommentStatus;

namespace MZikmund.Web.Core.Tests.Features.Comments;

public class GetModerationFeedHandlerTests
{
	private readonly Guid _postId = Guid.NewGuid();
	private readonly Guid _authorId = Guid.NewGuid();

	[Fact]
	public async Task Handle_WithoutSince_ReturnsEveryCommentNewestFirst()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		var ids = await SeedThreeAsync(db);
		var handler = new GetModerationFeedHandler(db);

		// Act
		var result = await handler.Handle(new GetModerationFeedQuery(null, 1, 20), CancellationToken.None);

		// Assert
		Assert.Equal(new[] { ids[2], ids[1], ids[0] }, result.Data.Select(c => c.Id));
		Assert.Equal(3, result.TotalCount);
	}

	[Fact]
	public async Task Handle_WithSince_ReturnsOnlyStrictlyNewerComments()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		var ids = await SeedThreeAsync(db);
		var handler = new GetModerationFeedHandler(db);

		// Act
		var result = await handler.Handle(new GetModerationFeedQuery(Day(10), 1, 20), CancellationToken.None);

		// Assert
		Assert.Equal(new[] { ids[2], ids[1] }, result.Data.Select(c => c.Id));
		Assert.Equal(2, result.TotalCount);
	}

	[Fact]
	public async Task Handle_HiddenComment_IsListedButItsTextStaysBlank()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		AddPost(db, _postId);
		AddAuthor(db, _authorId, "Commenter");
		var comment = AddComment(db, _postId, _authorId, "removed words", status: DataCommentStatus.HiddenByModerator);
		await db.SaveChangesAsync(CancellationToken.None);
		var handler = new GetModerationFeedHandler(db);

		// Act
		var result = await handler.Handle(new GetModerationFeedQuery(null, 1, 20), CancellationToken.None);

		// Assert
		var row = Assert.Single(result.Data);
		Assert.Equal(comment.Id, row.Id);
		Assert.Equal(CommentStatus.HiddenByModerator, row.Status);
		Assert.Equal("", row.Content);
		Assert.Equal("Commenter", row.AuthorDisplayName);
		Assert.False(row.HasUpvoted);
		Assert.False(row.IsOwn);
		Assert.Empty(row.Replies);
	}

	[Fact]
	public async Task Handle_SecondPage_SkipsTheFirstPageAndKeepsTheTotal()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		var ids = await SeedThreeAsync(db);
		var handler = new GetModerationFeedHandler(db);

		// Act
		var result = await handler.Handle(new GetModerationFeedQuery(null, 2, 2), CancellationToken.None);

		// Assert
		Assert.Equal(ids[0], Assert.Single(result.Data).Id);
		Assert.Equal(3, result.TotalCount);
		Assert.Equal(2, result.PageNumber);
		Assert.Equal(2, result.PageSize);
	}

	[Theory]
	[InlineData(0, 20)]
	[InlineData(1, 0)]
	public async Task Handle_InvalidPaging_ThrowsArgumentOutOfRangeException(int pageNumber, int pageSize)
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		var handler = new GetModerationFeedHandler(db);

		// Act & Assert
		await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
			() => handler.Handle(new GetModerationFeedQuery(null, pageNumber, pageSize), CancellationToken.None));
	}

	private async Task<Guid[]> SeedThreeAsync(DatabaseContext db)
	{
		AddPost(db, _postId);
		AddAuthor(db, _authorId, "Commenter");
		Guid[] ids =
		[
			AddComment(db, _postId, _authorId, createdOn: Day(10)).Id,
			AddComment(db, _postId, _authorId, createdOn: Day(11)).Id,
			AddComment(db, _postId, _authorId, createdOn: Day(12)).Id,
		];
		await db.SaveChangesAsync(CancellationToken.None);
		return ids;
	}
}
