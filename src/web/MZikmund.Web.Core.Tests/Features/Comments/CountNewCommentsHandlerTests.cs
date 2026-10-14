using MZikmund.Web.Core.Features.Comments;
using MZikmund.Web.Core.Tests.Infrastructure;
using MZikmund.Web.Data;
using static MZikmund.Web.Core.Tests.Features.Comments.CommentSeed;
using DataCommentStatus = MZikmund.Web.Data.Entities.CommentStatus;

namespace MZikmund.Web.Core.Tests.Features.Comments;

public class CountNewCommentsHandlerTests
{
	[Fact]
	public async Task Handle_WithoutSince_CountsEveryCommentIncludingHiddenOnes()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		await SeedAsync(db);
		var handler = new CountNewCommentsHandler(db);

		// Act
		var count = await handler.Handle(new CountNewCommentsQuery(null), CancellationToken.None);

		// Assert
		Assert.Equal(3, count);
	}

	[Fact]
	public async Task Handle_WithSince_CountsOnlyStrictlyNewerComments()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		await SeedAsync(db);
		var handler = new CountNewCommentsHandler(db);

		// Act
		var count = await handler.Handle(new CountNewCommentsQuery(Day(11)), CancellationToken.None);

		// Assert
		Assert.Equal(1, count);
	}

	[Fact]
	public async Task Handle_WithSinceAfterEverything_ReturnsZero()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		await SeedAsync(db);
		var handler = new CountNewCommentsHandler(db);

		// Act
		var count = await handler.Handle(new CountNewCommentsQuery(Day(20)), CancellationToken.None);

		// Assert
		Assert.Equal(0, count);
	}

	[Fact]
	public async Task Handle_WithSince_AgreesWithTheModerationFeedTotal()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		await SeedAsync(db);

		// Act
		var count = await new CountNewCommentsHandler(db)
			.Handle(new CountNewCommentsQuery(Day(10)), CancellationToken.None);
		var feed = await new GetModerationFeedHandler(db)
			.Handle(new GetModerationFeedQuery(Day(10), 1, 1), CancellationToken.None);

		// Assert
		Assert.Equal(feed.TotalCount, count);
	}

	private static async Task SeedAsync(DatabaseContext db)
	{
		var postId = Guid.NewGuid();
		var authorId = Guid.NewGuid();
		AddPost(db, postId);
		AddAuthor(db, authorId, "Commenter");
		AddComment(db, postId, authorId, createdOn: Day(10));
		AddComment(db, postId, authorId, createdOn: Day(11), status: DataCommentStatus.HiddenByModerator);
		AddComment(db, postId, authorId, createdOn: Day(12));
		await db.SaveChangesAsync(CancellationToken.None);
	}
}
