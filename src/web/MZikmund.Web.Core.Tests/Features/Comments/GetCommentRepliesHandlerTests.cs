using MZikmund.Web.Core.Features.Comments;
using MZikmund.Web.Core.Services.Comments;
using MZikmund.Web.Core.Tests.Infrastructure;
using MZikmund.Web.Data;
using MZikmund.Web.Data.Entities;
using static MZikmund.Web.Core.Tests.Features.Comments.CommentSeed;
using DataCommentStatus = MZikmund.Web.Data.Entities.CommentStatus;

namespace MZikmund.Web.Core.Tests.Features.Comments;

public class GetCommentRepliesHandlerTests
{
	[Fact]
	public async Task Handle_SecondPage_ReturnsRepliesInCreatedOnAscendingOrder()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var context = fixture.CreateContext();
		var postId = Guid.NewGuid();
		var authorId = Guid.NewGuid();
		AddPost(context, postId);
		AddAuthor(context, authorId, "Homer");
		var parent = AddComment(context, postId, authorId, "parent", Day(1), replyCount: 5);
		for (var index = 1; index <= 5; index++)
		{
			AddComment(context, postId, authorId, $"reply {index}", Day(1).AddMinutes(index), parentId: parent.Id);
		}

		await context.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(context);

		// Act
		var result = await handler.Handle(new GetCommentRepliesQuery(parent.Id, 2, 3), CancellationToken.None);

		// Assert
		Assert.Equal(5, result.TotalCount);
		Assert.Equal(2, result.PageNumber);
		Assert.Equal(3, result.PageSize);
		var replies = result.Data.ToArray();
		Assert.Equal(2, replies.Length);
		Assert.Equal("reply 4", replies[0].Content);
		Assert.Equal("reply 5", replies[1].Content);
	}

	[Fact]
	public async Task Handle_WithSiblingThread_ReturnsRepliesOfTheRequestedCommentOnly()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var context = fixture.CreateContext();
		var postId = Guid.NewGuid();
		var authorId = Guid.NewGuid();
		AddPost(context, postId);
		AddAuthor(context, authorId, "Marge");
		var wanted = AddComment(context, postId, authorId, "wanted", Day(1), replyCount: 1);
		var other = AddComment(context, postId, authorId, "other", Day(2), replyCount: 1);
		AddComment(context, postId, authorId, "mine", Day(3), parentId: wanted.Id);
		AddComment(context, postId, authorId, "not mine", Day(4), parentId: other.Id);
		await context.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(context);

		// Act
		var result = await handler.Handle(new GetCommentRepliesQuery(wanted.Id, 1, 20), CancellationToken.None);

		// Assert
		Assert.Equal(1, result.TotalCount);
		var reply = Assert.Single(result.Data);
		Assert.Equal("mine", reply.Content);
		Assert.Equal(wanted.Id, reply.ParentId);
		Assert.Empty(reply.Replies);
	}

	[Fact]
	public async Task Handle_WithHiddenReply_ExcludesItFromDataAndTotalCount()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var context = fixture.CreateContext();
		var postId = Guid.NewGuid();
		var authorId = Guid.NewGuid();
		AddPost(context, postId);
		AddAuthor(context, authorId, "Lisa");
		var parent = AddComment(context, postId, authorId, "parent", Day(1), replyCount: 2);
		AddComment(context, postId, authorId, "hidden", Day(2), parentId: parent.Id, status: DataCommentStatus.HiddenByModerator);
		AddComment(context, postId, authorId, "shown", Day(3), parentId: parent.Id);
		await context.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(context);

		// Act
		var result = await handler.Handle(new GetCommentRepliesQuery(parent.Id, 1, 20), CancellationToken.None);

		// Assert
		Assert.Equal(1, result.TotalCount);
		var reply = Assert.Single(result.Data);
		Assert.Equal("shown", reply.Content);
	}

	[Fact]
	public async Task Handle_WithDeletedParent_StillServesItsReplies()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var context = fixture.CreateContext();
		var postId = Guid.NewGuid();
		var authorId = Guid.NewGuid();
		AddPost(context, postId);
		AddAuthor(context, authorId, "Bart");
		var parent = AddComment(context, postId, authorId, "gone", Day(1), replyCount: 1, status: DataCommentStatus.DeletedByAuthor);
		AddComment(context, postId, authorId, "survivor", Day(2), parentId: parent.Id);
		await context.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(context);

		// Act
		var result = await handler.Handle(new GetCommentRepliesQuery(parent.Id, 1, 20), CancellationToken.None);

		// Assert
		var reply = Assert.Single(result.Data);
		Assert.Equal("survivor", reply.Content);
	}

	[Fact]
	public async Task Handle_ForTheVotingAuthor_SetsHasUpvotedAndIsOwn()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var context = fixture.CreateContext();
		var postId = Guid.NewGuid();
		var authorId = Guid.NewGuid();
		AddPost(context, postId);
		AddAuthor(context, authorId, "Bart");
		var parent = AddComment(context, postId, authorId, "parent", Day(1), replyCount: 1);
		var reply = AddComment(context, postId, authorId, "reply", Day(2), upvoteCount: 1, parentId: parent.Id);
		context.CommentVote.Add(new CommentVoteEntity
		{
			CommentId = reply.Id,
			AuthorId = authorId,
			CreatedOn = Day(2),
		});
		await context.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(context, authorId);

		// Act
		var result = await handler.Handle(new GetCommentRepliesQuery(parent.Id, 1, 20), CancellationToken.None);

		// Assert
		var loaded = Assert.Single(result.Data);
		Assert.True(loaded.HasUpvoted);
		Assert.True(loaded.IsOwn);
	}

	[Fact]
	public async Task Handle_DeletedReplies_AreLeftOutOfBothTheItemsAndTheTotal()
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
		var result = await handler.Handle(new GetCommentRepliesQuery(parent.Id, 1, 20), CancellationToken.None);

		// Assert
		Assert.Equal(1, result.TotalCount);
		Assert.Equal("kept", Assert.Single(result.Data).Content);
	}

	[Fact]
	public async Task Handle_HiddenParentWithVisibleReplies_StillServesThoseReplies()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var context = fixture.CreateContext();
		var postId = Guid.NewGuid();
		var authorId = Guid.NewGuid();
		AddPost(context, postId);
		AddAuthor(context, authorId, "Lisa");
		var parent = AddComment(context, postId, authorId, "hidden", Day(1), replyCount: 1, status: DataCommentStatus.HiddenByModerator);
		AddComment(context, postId, authorId, "survivor", Day(2), parentId: parent.Id);
		await context.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(context);

		// Act
		var result = await handler.Handle(new GetCommentRepliesQuery(parent.Id, 1, 20), CancellationToken.None);

		// Assert
		Assert.Equal(1, result.TotalCount);
		Assert.Equal("survivor", Assert.Single(result.Data).Content);
	}

	[Fact]
	public async Task Handle_WithUnknownComment_ThrowsKeyNotFound()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var context = fixture.CreateContext();
		var handler = CreateHandler(context);

		// Act & Assert
		await Assert.ThrowsAsync<CommentNotFoundException>(
			() => handler.Handle(new GetCommentRepliesQuery(Guid.NewGuid(), 1, 20), CancellationToken.None));
	}

	[Fact]
	public async Task Handle_WithHiddenParentAndNothingVisibleUnderIt_ThrowsNotFound()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var context = fixture.CreateContext();
		var postId = Guid.NewGuid();
		var authorId = Guid.NewGuid();
		AddPost(context, postId);
		AddAuthor(context, authorId, "Nelson");
		var parent = AddComment(context, postId, authorId, "hidden", Day(1), replyCount: 1, status: DataCommentStatus.HiddenByModerator);
		AddComment(context, postId, authorId, "reply", Day(2), parentId: parent.Id, status: DataCommentStatus.HiddenByModerator);
		await context.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(context);

		// Act & Assert
		await Assert.ThrowsAsync<CommentNotFoundException>(
			() => handler.Handle(new GetCommentRepliesQuery(parent.Id, 1, 20), CancellationToken.None));
	}

	[Fact]
	public async Task Handle_WithParentOnDraftPost_ThrowsKeyNotFound()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var context = fixture.CreateContext();
		var postId = Guid.NewGuid();
		var authorId = Guid.NewGuid();
		AddPost(context, postId, PostStatus.Draft);
		AddAuthor(context, authorId, "Ralph");
		var parent = AddComment(context, postId, authorId, "parent", Day(1), replyCount: 1);
		AddComment(context, postId, authorId, "reply", Day(2), parentId: parent.Id);
		await context.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(context);

		// Act & Assert
		await Assert.ThrowsAsync<CommentNotFoundException>(
			() => handler.Handle(new GetCommentRepliesQuery(parent.Id, 1, 20), CancellationToken.None));
	}

	[Fact]
	public async Task Handle_WithPageSizeBelowOne_Throws()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var context = fixture.CreateContext();
		var handler = CreateHandler(context);
		var query = new GetCommentRepliesQuery(Guid.NewGuid(), 1, 0);

		// Act & Assert
		await Assert.ThrowsAsync<CommentValidationException>(() => handler.Handle(query, CancellationToken.None));
	}

	private static GetCommentRepliesHandler CreateHandler(DatabaseContext context, Guid? currentAuthorId = null) =>
		new(context, CurrentUser(currentAuthorId).Object, FixedClock().Object);
}
