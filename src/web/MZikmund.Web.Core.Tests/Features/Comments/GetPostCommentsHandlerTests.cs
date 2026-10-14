using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Web.Core.Features.Comments;
using MZikmund.Web.Core.Services.Comments;
using MZikmund.Web.Core.Tests.Infrastructure;
using MZikmund.Web.Data;
using MZikmund.Web.Data.Entities;
using static MZikmund.Web.Core.Tests.Features.Comments.CommentSeed;
using DataCommentStatus = MZikmund.Web.Data.Entities.CommentStatus;
using DtoCommentStatus = MZikmund.DataContracts.Blog.Comments.CommentStatus;

namespace MZikmund.Web.Core.Tests.Features.Comments;

public class GetPostCommentsHandlerTests
{
	[Fact]
	public async Task Handle_SortTop_OrdersByUpvoteCountDescendingThenCreatedOnAscending()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var context = fixture.CreateContext();
		var postId = Guid.NewGuid();
		var authorId = Guid.NewGuid();
		AddPost(context, postId);
		AddAuthor(context, authorId, "Homer");
		var quiet = AddComment(context, postId, authorId, "quiet", Day(1), upvoteCount: 1);
		var late = AddComment(context, postId, authorId, "late", Day(3), upvoteCount: 9);
		var early = AddComment(context, postId, authorId, "early", Day(2), upvoteCount: 9);
		await context.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(context);

		// Act
		var result = await handler.Handle(new GetPostCommentsQuery(postId, CommentSortOrder.Top, 1, 20), CancellationToken.None);

		// Assert
		var comments = result.Data.ToArray();
		Assert.Equal(3, comments.Length);
		Assert.Equal(early.Id, comments[0].Id);
		Assert.Equal(late.Id, comments[1].Id);
		Assert.Equal(quiet.Id, comments[2].Id);
	}

	[Fact]
	public async Task Handle_DeletedReply_IsLeftOutSoReplyCountAndInlineRepliesAgree()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var context = fixture.CreateContext();
		var postId = Guid.NewGuid();
		var authorId = Guid.NewGuid();
		AddPost(context, postId);
		AddAuthor(context, authorId, "Homer");
		var parent = AddComment(context, postId, authorId, "parent", Day(1), replyCount: 1);
		AddComment(context, postId, authorId, "kept", Day(2), parentId: parent.Id);
		AddComment(context, postId, authorId, "", Day(3), parentId: parent.Id, status: DataCommentStatus.DeletedByAuthor);
		await context.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(context);

		// Act
		var result = await handler.Handle(new GetPostCommentsQuery(postId, CommentSortOrder.New, 1, 20), CancellationToken.None);

		// Assert
		var comment = Assert.Single(result.Data);
		Assert.Equal(1, comment.ReplyCount);
		var reply = Assert.Single(comment.Replies);
		Assert.Equal("kept", reply.Content);
	}

	[Fact]
	public async Task Handle_HiddenTopLevelCommentWithVisibleReplies_ShowsAsTombstoneKeepingTheReplies()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var context = fixture.CreateContext();
		var postId = Guid.NewGuid();
		var authorId = Guid.NewGuid();
		AddPost(context, postId);
		AddAuthor(context, authorId, "Marge");
		var hidden = AddComment(context, postId, authorId, "spicy take", Day(1), replyCount: 1, status: DataCommentStatus.HiddenByModerator);
		AddComment(context, postId, authorId, "good reply", Day(2), parentId: hidden.Id);
		await context.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(context);

		// Act
		var result = await handler.Handle(new GetPostCommentsQuery(postId, CommentSortOrder.New, 1, 20), CancellationToken.None);

		// Assert
		Assert.Equal(1, result.TotalCount);
		var tombstone = Assert.Single(result.Data);
		Assert.Equal(DtoCommentStatus.HiddenByModerator, tombstone.Status);
		Assert.Equal("", tombstone.Content);
		Assert.Equal("", tombstone.ContentHtml);
		Assert.Equal("good reply", Assert.Single(tombstone.Replies).Content);
	}

	[Theory]
	[InlineData(DataCommentStatus.HiddenByModerator)]
	[InlineData(DataCommentStatus.DeletedByAuthor)]
	public async Task Handle_HiddenTopLevelCommentWithNothingVisibleUnderIt_IsExcluded(DataCommentStatus replyStatus)
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var context = fixture.CreateContext();
		var postId = Guid.NewGuid();
		var authorId = Guid.NewGuid();
		AddPost(context, postId);
		AddAuthor(context, authorId, "Bart");
		var hidden = AddComment(context, postId, authorId, "spicy take", Day(1), status: DataCommentStatus.HiddenByModerator);
		AddComment(context, postId, authorId, "gone", Day(2), parentId: hidden.Id, status: replyStatus);
		await context.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(context);

		// Act
		var result = await handler.Handle(new GetPostCommentsQuery(postId, CommentSortOrder.New, 1, 20), CancellationToken.None);

		// Assert
		Assert.Equal(0, result.TotalCount);
		Assert.Empty(result.Data);
	}

	[Fact]
	public async Task Handle_CommentsWithTheSameTimestamp_PageDeterministicallyWithoutGapsOrRepeats()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var context = fixture.CreateContext();
		var postId = Guid.NewGuid();
		var authorId = Guid.NewGuid();
		AddPost(context, postId);
		AddAuthor(context, authorId, "Homer");
		for (var index = 0; index < 7; index++)
		{
			AddComment(context, postId, authorId, $"c{index}", Day(1), upvoteCount: 3);
		}

		await context.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(context);

		foreach (var sort in new[] { CommentSortOrder.New, CommentSortOrder.Top })
		{
			// Act
			List<Guid> seen = new();
			for (var page = 1; page <= 3; page++)
			{
				var result = await handler.Handle(new GetPostCommentsQuery(postId, sort, page, 3), CancellationToken.None);
				seen.AddRange(result.Data.Select(c => c.Id));
			}

			// Assert
			Assert.Equal(7, seen.Count);
			Assert.Equal(7, seen.Distinct().Count());
		}
	}

	[Theory]
	[InlineData(0)]
	[InlineData(10_001)]
	[InlineData(int.MaxValue)]
	public async Task Handle_PageNumberOutOfRange_ThrowsValidation(int pageNumber)
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var context = fixture.CreateContext();
		var handler = CreateHandler(context);
		var query = new GetPostCommentsQuery(Guid.NewGuid(), CommentSortOrder.New, pageNumber, 20);

		// Act & Assert
		await Assert.ThrowsAsync<CommentValidationException>(() => handler.Handle(query, CancellationToken.None));
	}

	[Fact]
	public async Task Handle_SortNew_OrdersByCreatedOnDescending()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var context = fixture.CreateContext();
		var postId = Guid.NewGuid();
		var authorId = Guid.NewGuid();
		AddPost(context, postId);
		AddAuthor(context, authorId, "Marge");
		var oldest = AddComment(context, postId, authorId, "oldest", Day(1), upvoteCount: 50);
		var middle = AddComment(context, postId, authorId, "middle", Day(2));
		var newest = AddComment(context, postId, authorId, "newest", Day(3));
		await context.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(context);

		// Act
		var result = await handler.Handle(new GetPostCommentsQuery(postId, CommentSortOrder.New, 1, 20), CancellationToken.None);

		// Assert
		var comments = result.Data.ToArray();
		Assert.Equal(newest.Id, comments[0].Id);
		Assert.Equal(middle.Id, comments[1].Id);
		Assert.Equal(oldest.Id, comments[2].Id);
	}

	[Fact]
	public async Task Handle_SecondPage_ReturnsTopLevelOnlyAndCountsTopLevelOnly()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var context = fixture.CreateContext();
		var postId = Guid.NewGuid();
		var authorId = Guid.NewGuid();
		AddPost(context, postId);
		AddAuthor(context, authorId, "Lisa");
		var first = AddComment(context, postId, authorId, "first", Day(3), replyCount: 2);
		AddComment(context, postId, authorId, "second", Day(2));
		var third = AddComment(context, postId, authorId, "third", Day(1));
		AddComment(context, postId, authorId, "reply a", Day(4), parentId: first.Id);
		AddComment(context, postId, authorId, "reply b", Day(5), parentId: first.Id);
		await context.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(context);

		// Act
		var result = await handler.Handle(new GetPostCommentsQuery(postId, CommentSortOrder.New, 2, 2), CancellationToken.None);

		// Assert
		Assert.Equal(3, result.TotalCount);
		Assert.Equal(2, result.PageNumber);
		Assert.Equal(2, result.PageSize);
		var comment = Assert.Single(result.Data);
		Assert.Equal(third.Id, comment.Id);
	}

	[Fact]
	public async Task Handle_WithMoreThanThreeReplies_EmbedsFirstThreeAscendingAndKeepsTheFullReplyCount()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var context = fixture.CreateContext();
		var postId = Guid.NewGuid();
		var authorId = Guid.NewGuid();
		AddPost(context, postId);
		AddAuthor(context, authorId, "Bart");
		var parent = AddComment(context, postId, authorId, "parent", Day(1), replyCount: 5);
		for (var index = 1; index <= 5; index++)
		{
			AddComment(context, postId, authorId, $"reply {index}", Day(1).AddMinutes(index), parentId: parent.Id);
		}

		await context.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(context);

		// Act
		var result = await handler.Handle(new GetPostCommentsQuery(postId, CommentSortOrder.Top, 1, 20), CancellationToken.None);

		// Assert
		var comment = Assert.Single(result.Data);
		Assert.Equal(5, comment.ReplyCount);
		Assert.Equal(3, comment.Replies.Length);
		Assert.Equal("reply 1", comment.Replies[0].Content);
		Assert.Equal("reply 2", comment.Replies[1].Content);
		Assert.Equal("reply 3", comment.Replies[2].Content);
	}

	[Fact]
	public async Task Handle_ForTheVotingAuthor_SetsHasUpvotedAndIsOwn()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var context = fixture.CreateContext();
		var postId = Guid.NewGuid();
		var authorId = Guid.NewGuid();
		var strangerId = Guid.NewGuid();
		AddPost(context, postId);
		AddAuthor(context, authorId, "Ned");
		AddAuthor(context, strangerId, "Moe");
		var mine = AddComment(context, postId, authorId, "mine", Day(2), upvoteCount: 1);
		AddComment(context, postId, strangerId, "theirs", Day(1));
		context.CommentVote.Add(new CommentVoteEntity
		{
			CommentId = mine.Id,
			AuthorId = authorId,
			CreatedOn = Day(2),
		});
		await context.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(context, authorId);

		// Act
		var result = await handler.Handle(new GetPostCommentsQuery(postId, CommentSortOrder.New, 1, 20), CancellationToken.None);

		// Assert
		var comments = result.Data.ToArray();
		Assert.True(comments[0].HasUpvoted);
		Assert.True(comments[0].IsOwn);
		Assert.False(comments[1].HasUpvoted);
		Assert.False(comments[1].IsOwn);
	}

	[Fact]
	public async Task Handle_WithHiddenComment_ExcludesItFromDataAndTotalCount()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var context = fixture.CreateContext();
		var postId = Guid.NewGuid();
		var authorId = Guid.NewGuid();
		AddPost(context, postId);
		AddAuthor(context, authorId, "Krusty");
		var shown = AddComment(context, postId, authorId, "shown", Day(1));
		AddComment(context, postId, authorId, "hidden", Day(2), status: DataCommentStatus.HiddenByModerator);
		await context.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(context);

		// Act
		var result = await handler.Handle(new GetPostCommentsQuery(postId, CommentSortOrder.New, 1, 20), CancellationToken.None);

		// Assert
		Assert.Equal(1, result.TotalCount);
		var comment = Assert.Single(result.Data);
		Assert.Equal(shown.Id, comment.Id);
	}

	[Fact]
	public async Task Handle_WithDeletedComment_ReturnsATombstoneWithoutTheText()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var context = fixture.CreateContext();
		var postId = Guid.NewGuid();
		var authorId = Guid.NewGuid();
		AddPost(context, postId);
		AddAuthor(context, authorId, "Apu");
		AddComment(context, postId, authorId, "secret words", Day(1), status: DataCommentStatus.DeletedByAuthor);
		await context.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(context);

		// Act
		var result = await handler.Handle(new GetPostCommentsQuery(postId, CommentSortOrder.New, 1, 20), CancellationToken.None);

		// Assert
		var comment = Assert.Single(result.Data);
		Assert.Equal(MZikmund.DataContracts.Blog.Comments.CommentStatus.DeletedByAuthor, comment.Status);
		Assert.Equal("", comment.Content);
		Assert.Equal("", comment.ContentHtml);
	}

	[Fact]
	public async Task Handle_WithDraftPost_ThrowsKeyNotFound()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var context = fixture.CreateContext();
		var postId = Guid.NewGuid();
		AddPost(context, postId, PostStatus.Draft);
		await context.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(context);

		// Act & Assert
		await Assert.ThrowsAsync<CommentNotFoundException>(
			() => handler.Handle(new GetPostCommentsQuery(postId, CommentSortOrder.New, 1, 20), CancellationToken.None));
	}

	[Fact]
	public async Task Handle_WithFutureDatedPost_ThrowsKeyNotFound()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var context = fixture.CreateContext();
		var postId = Guid.NewGuid();
		AddPost(context, postId, publishedDate: DateTimeOffset.UtcNow.AddDays(30));
		await context.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(context);

		// Act & Assert
		await Assert.ThrowsAsync<CommentNotFoundException>(
			() => handler.Handle(new GetPostCommentsQuery(postId, CommentSortOrder.New, 1, 20), CancellationToken.None));
	}

	[Fact]
	public async Task Handle_WithUnknownPost_ThrowsKeyNotFound()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var context = fixture.CreateContext();
		var handler = CreateHandler(context);

		// Act & Assert
		await Assert.ThrowsAsync<CommentNotFoundException>(
			() => handler.Handle(new GetPostCommentsQuery(Guid.NewGuid(), CommentSortOrder.New, 1, 20), CancellationToken.None));
	}

	[Fact]
	public async Task Handle_WithPageNumberBelowOne_Throws()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var context = fixture.CreateContext();
		var handler = CreateHandler(context);
		var query = new GetPostCommentsQuery(Guid.NewGuid(), CommentSortOrder.New, 0, 20);

		// Act & Assert
		await Assert.ThrowsAsync<CommentValidationException>(() => handler.Handle(query, CancellationToken.None));
	}

	private static GetPostCommentsHandler CreateHandler(DatabaseContext context, Guid? currentAuthorId = null) =>
		new(context, CurrentUser(currentAuthorId).Object, FixedClock().Object);
}
