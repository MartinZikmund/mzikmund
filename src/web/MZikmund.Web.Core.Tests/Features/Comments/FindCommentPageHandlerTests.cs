using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Web.Core.Features.Comments;
using MZikmund.Web.Core.Tests.Infrastructure;
using MZikmund.Web.Data;
using MZikmund.Web.Data.Entities;
using static MZikmund.Web.Core.Tests.Features.Comments.CommentSeed;
using DataCommentStatus = MZikmund.Web.Data.Entities.CommentStatus;

namespace MZikmund.Web.Core.Tests.Features.Comments;

public class FindCommentPageHandlerTests
{
	private static readonly Guid PostId = Guid.NewGuid();
	private static readonly Guid AuthorId = Guid.NewGuid();

	[Fact]
	public async Task Handle_UnknownComment_ReturnsNull()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var context = fixture.CreateContext();
		var handler = new FindCommentPageHandler(context);

		// Act
		var page = await handler.Handle(new FindCommentPageQuery(Guid.NewGuid(), CommentSortOrder.New, 20), CancellationToken.None);

		// Assert
		Assert.Null(page);
	}

	[Fact]
	public async Task Handle_NewSortTwentyFifthNewest_ReturnsPageTwo()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var context = fixture.CreateContext();
		Seed(context);
		CommentEntity target = null!;
		for (var index = 0; index < 25; index++)
		{
			var comment = AddComment(context, PostId, AuthorId, createdOn: Day(1).AddMinutes(index));
			if (index == 4)
			{
				target = comment;
			}
		}

		await context.SaveChangesAsync(CancellationToken.None);
		var handler = new FindCommentPageHandler(context);

		// Act
		var page = await handler.Handle(new FindCommentPageQuery(target.Id, CommentSortOrder.New, 20), CancellationToken.None);

		// Assert
		Assert.Equal(2, page);
	}

	[Fact]
	public async Task Handle_Reply_ReturnsPageOfItsParent()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var context = fixture.CreateContext();
		Seed(context);
		CommentEntity parent = null!;
		for (var index = 0; index < 25; index++)
		{
			var comment = AddComment(context, PostId, AuthorId, createdOn: Day(1).AddMinutes(index), replyCount: index == 0 ? 1 : 0);
			if (index == 0)
			{
				parent = comment;
			}
		}

		var reply = AddComment(context, PostId, AuthorId, createdOn: Day(2), parentId: parent.Id);
		await context.SaveChangesAsync(CancellationToken.None);
		var handler = new FindCommentPageHandler(context);

		// Act
		var page = await handler.Handle(new FindCommentPageQuery(reply.Id, CommentSortOrder.New, 20), CancellationToken.None);

		// Assert
		Assert.Equal(2, page);
	}

	[Fact]
	public async Task Handle_TopSort_OrdersByUpvotesThenOldestFirst()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var context = fixture.CreateContext();
		Seed(context);
		AddComment(context, PostId, AuthorId, createdOn: Day(1), upvoteCount: 10);
		AddComment(context, PostId, AuthorId, createdOn: Day(2), upvoteCount: 10);
		var target = AddComment(context, PostId, AuthorId, createdOn: Day(3), upvoteCount: 10);
		AddComment(context, PostId, AuthorId, createdOn: Day(4), upvoteCount: 99);
		await context.SaveChangesAsync(CancellationToken.None);
		var handler = new FindCommentPageHandler(context);

		// Act
		var page = await handler.Handle(new FindCommentPageQuery(target.Id, CommentSortOrder.Top, 3), CancellationToken.None);

		// Assert
		Assert.Equal(2, page);
	}

	[Fact]
	public async Task Handle_HiddenWithoutVisibleReplies_ReturnsNull()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var context = fixture.CreateContext();
		Seed(context);
		var hidden = AddComment(context, PostId, AuthorId, status: DataCommentStatus.HiddenByModerator);
		await context.SaveChangesAsync(CancellationToken.None);
		var handler = new FindCommentPageHandler(context);

		// Act
		var page = await handler.Handle(new FindCommentPageQuery(hidden.Id, CommentSortOrder.New, 20), CancellationToken.None);

		// Assert
		Assert.Null(page);
	}

	[Fact]
	public async Task Handle_HiddenTombstoneWithVisibleReply_StillCountsTowardsPaging()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var context = fixture.CreateContext();
		Seed(context);
		var oldest = AddComment(context, PostId, AuthorId, createdOn: Day(1));
		var tombstone = AddComment(context, PostId, AuthorId, createdOn: Day(2), replyCount: 1, status: DataCommentStatus.HiddenByModerator);
		AddComment(context, PostId, AuthorId, createdOn: Day(2).AddMinutes(1), parentId: tombstone.Id);
		await context.SaveChangesAsync(CancellationToken.None);
		var handler = new FindCommentPageHandler(context);

		// Act
		var page = await handler.Handle(new FindCommentPageQuery(oldest.Id, CommentSortOrder.New, 1), CancellationToken.None);

		// Assert
		Assert.Equal(2, page);
	}

	private static void Seed(DatabaseContext context)
	{
		AddPost(context, PostId);
		AddAuthor(context, AuthorId, "Reader");
	}
}
