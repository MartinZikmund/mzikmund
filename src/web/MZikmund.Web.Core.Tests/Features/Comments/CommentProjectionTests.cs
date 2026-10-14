using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using MZikmund.Web.Core.Features.Comments;
using MZikmund.Web.Core.Tests.Infrastructure;
using MZikmund.Web.Data;
using MZikmund.Web.Data.Entities;
using DataCommentStatus = MZikmund.Web.Data.Entities.CommentStatus;
using DtoCommentStatus = MZikmund.DataContracts.Blog.Comments.CommentStatus;

namespace MZikmund.Web.Core.Tests.Features.Comments;

public class CommentProjectionTests : IClassFixture<CommentsDbFixture>
{
	private readonly CommentsDbFixture _fixture;

	public CommentProjectionTests(CommentsDbFixture fixture)
	{
		_fixture = fixture;
	}

	[Fact]
	public async Task SelectRows_ForVotingAuthor_SetsHasUpvotedAndIsOwn()
	{
		// Arrange
		using var context = _fixture.CreateContext();
		var postId = Guid.NewGuid();
		var authorId = Guid.NewGuid();
		SeedPost(context, postId);
		SeedAuthor(context, authorId, "Marge");
		var comment = SeedComment(context, postId, authorId, "hello", Day(1), upvoteCount: 1);
		context.CommentVote.Add(new CommentVoteEntity
		{
			CommentId = comment.Id,
			AuthorId = authorId,
			CreatedOn = Day(1),
		});
		await context.SaveChangesAsync(CancellationToken.None);

		// Act
		var rows = await CommentProjection
			.SelectRows(context.Comment.Where(c => c.PostId == postId), authorId)
			.ToArrayAsync(CancellationToken.None);

		// Assert
		var row = Assert.Single(rows);
		Assert.True(row.HasUpvoted);
		Assert.True(row.IsOwn);
		Assert.Equal("Marge", row.AuthorDisplayName);
		Assert.Equal(1, row.UpvoteCount);
	}

	[Fact]
	public async Task SelectRows_ForOtherAuthor_LeavesHasUpvotedAndIsOwnFalse()
	{
		// Arrange
		using var context = _fixture.CreateContext();
		var postId = Guid.NewGuid();
		var authorId = Guid.NewGuid();
		var readerId = Guid.NewGuid();
		SeedPost(context, postId);
		SeedAuthor(context, authorId, "Homer");
		SeedAuthor(context, readerId, "Lisa");
		var comment = SeedComment(context, postId, authorId, "donuts", Day(1));
		context.CommentVote.Add(new CommentVoteEntity
		{
			CommentId = comment.Id,
			AuthorId = authorId,
			CreatedOn = Day(1),
		});
		await context.SaveChangesAsync(CancellationToken.None);

		// Act
		var rows = await CommentProjection
			.SelectRows(context.Comment.Where(c => c.PostId == postId), readerId)
			.ToArrayAsync(CancellationToken.None);

		// Assert
		var row = Assert.Single(rows);
		Assert.False(row.HasUpvoted);
		Assert.False(row.IsOwn);
	}

	[Fact]
	public async Task LoadInlineRepliesAsync_WithFiveReplies_ReturnsFirstThreeInCreatedOnOrder()
	{
		// Arrange
		using var context = _fixture.CreateContext();
		var postId = Guid.NewGuid();
		var authorId = Guid.NewGuid();
		SeedPost(context, postId);
		SeedAuthor(context, authorId, "Bart");
		var parent = SeedComment(context, postId, authorId, "parent", Day(1), replyCount: 5);
		for (var index = 1; index <= 5; index++)
		{
			SeedComment(context, postId, authorId, $"reply {index}", Day(1).AddMinutes(index), parentId: parent.Id);
		}

		await context.SaveChangesAsync(CancellationToken.None);

		// Act
		var repliesByParentId = await CommentProjection.LoadInlineRepliesAsync(
			context.Comment,
			new[] { parent.Id },
			currentAuthorId: null,
			CancellationToken.None);

		// Assert
		var replies = repliesByParentId[parent.Id];
		Assert.Equal(3, replies.Length);
		Assert.Equal("reply 1", replies[0].Content);
		Assert.Equal("reply 2", replies[1].Content);
		Assert.Equal("reply 3", replies[2].Content);
	}

	[Fact]
	public async Task LoadInlineRepliesAsync_WithHiddenReply_SkipsItAndAddsAnEmptyEntryForChildlessParents()
	{
		// Arrange
		using var context = _fixture.CreateContext();
		var postId = Guid.NewGuid();
		var authorId = Guid.NewGuid();
		SeedPost(context, postId);
		SeedAuthor(context, authorId, "Maggie");
		var parent = SeedComment(context, postId, authorId, "parent", Day(1), replyCount: 2);
		var childless = SeedComment(context, postId, authorId, "childless", Day(2));
		SeedComment(context, postId, authorId, "hidden", Day(1).AddMinutes(1), parentId: parent.Id, status: DataCommentStatus.HiddenByModerator);
		SeedComment(context, postId, authorId, "shown", Day(1).AddMinutes(2), parentId: parent.Id);
		await context.SaveChangesAsync(CancellationToken.None);

		// Act
		var repliesByParentId = await CommentProjection.LoadInlineRepliesAsync(
			context.Comment,
			new[] { parent.Id, childless.Id },
			currentAuthorId: null,
			CancellationToken.None);

		// Assert
		var reply = Assert.Single(repliesByParentId[parent.Id]);
		Assert.Equal("shown", reply.Content);
		Assert.Empty(repliesByParentId[childless.Id]);
	}

	[Fact]
	public void ToDto_WithoutReplies_MapsEveryMemberAndReturnsAnEmptyRepliesArray()
	{
		// Arrange
		CommentRow row = new(
			Id: Guid.NewGuid(),
			PostId: Guid.NewGuid(),
			ParentId: null,
			AuthorDisplayName: "Homer",
			AuthorAvatarUrl: "https://example.com/avatar.png",
			AuthorIsAdmin: true,
			MentionedDisplayName: "Lisa",
			Content: "d'oh",
			ContentHtml: "<p>d'oh</p>",
			Status: DataCommentStatus.Visible,
			CreatedOn: Day(1),
			EditedOn: Day(2),
			UpvoteCount: 7,
			ReplyCount: 4,
			HasUpvoted: true,
			IsOwn: true);

		// Act
		var dto = CommentProjection.ToDto(row);

		// Assert
		Assert.Equal(row.Id, dto.Id);
		Assert.Equal(row.PostId, dto.PostId);
		Assert.Null(dto.ParentId);
		Assert.Equal("Homer", dto.AuthorDisplayName);
		Assert.Equal("https://example.com/avatar.png", dto.AuthorAvatarUrl);
		Assert.True(dto.AuthorIsAdmin);
		Assert.Equal("Lisa", dto.MentionedDisplayName);
		Assert.Equal("d'oh", dto.Content);
		Assert.Equal("<p>d'oh</p>", dto.ContentHtml);
		Assert.Equal(DtoCommentStatus.Visible, dto.Status);
		Assert.Equal(Day(1), dto.CreatedOn);
		Assert.Equal(Day(2), dto.EditedOn);
		Assert.Equal(7, dto.UpvoteCount);
		Assert.Equal(4, dto.ReplyCount);
		Assert.True(dto.HasUpvoted);
		Assert.True(dto.IsOwn);
		Assert.Empty(dto.Replies);
	}

	[Fact]
	public void ToDto_FromEntityWithoutReplies_MapsEveryMemberAndResolvesCallerFlags()
	{
		// Arrange
		var authorId = Guid.NewGuid();
		var strangerId = Guid.NewGuid();
		CommentEntity comment = new()
		{
			Id = Guid.NewGuid(),
			PostId = Guid.NewGuid(),
			ParentId = null,
			AuthorId = authorId,
			Content = "d'oh",
			ContentHtml = "<p>d'oh</p>",
			MentionedDisplayName = "Lisa",
			Status = DataCommentStatus.Visible,
			CreatedOn = Day(1),
			EditedOn = Day(2),
			UpvoteCount = 7,
			ReplyCount = 4,
			Author = new CommentAuthorEntity
			{
				Id = authorId,
				DisplayName = "Homer",
				AvatarUrl = "https://example.com/avatar.png",
				Email = "homer@example.com",
				CreatedOn = Day(1),
				LastSeenOn = Day(1),
			},
		};
		comment.Votes.Add(new CommentVoteEntity
		{
			CommentId = comment.Id,
			AuthorId = authorId,
			CreatedOn = Day(1),
		});

		// Act
		var dto = CommentProjection.ToDto(comment, authorId, includeReplies: false);
		var strangerDto = CommentProjection.ToDto(comment, strangerId, includeReplies: false);

		// Assert
		Assert.Equal(comment.Id, dto.Id);
		Assert.Equal(comment.PostId, dto.PostId);
		Assert.Null(dto.ParentId);
		Assert.Equal("Homer", dto.AuthorDisplayName);
		Assert.Equal("https://example.com/avatar.png", dto.AuthorAvatarUrl);
		Assert.Equal("Lisa", dto.MentionedDisplayName);
		Assert.Equal("d'oh", dto.Content);
		Assert.Equal("<p>d'oh</p>", dto.ContentHtml);
		Assert.Equal(DtoCommentStatus.Visible, dto.Status);
		Assert.Equal(Day(1), dto.CreatedOn);
		Assert.Equal(Day(2), dto.EditedOn);
		Assert.Equal(7, dto.UpvoteCount);
		Assert.Equal(4, dto.ReplyCount);
		Assert.True(dto.HasUpvoted);
		Assert.True(dto.IsOwn);
		Assert.Empty(dto.Replies);
		Assert.False(strangerDto.HasUpvoted);
		Assert.False(strangerDto.IsOwn);
	}

	[Theory]
	[InlineData(DataCommentStatus.DeletedByAuthor, DtoCommentStatus.DeletedByAuthor)]
	[InlineData(DataCommentStatus.HiddenByModerator, DtoCommentStatus.HiddenByModerator)]
	public void ToDto_FromRowThatIsNotVisible_BlanksTheBodies(DataCommentStatus status, DtoCommentStatus expected)
	{
		// Arrange
		CommentRow row = new(
			Id: Guid.NewGuid(),
			PostId: Guid.NewGuid(),
			ParentId: null,
			AuthorDisplayName: "Homer",
			AuthorAvatarUrl: null,
			AuthorIsAdmin: false,
			MentionedDisplayName: null,
			Content: "secret",
			ContentHtml: "<p>secret</p>",
			Status: status,
			CreatedOn: Day(1),
			EditedOn: null,
			UpvoteCount: 0,
			ReplyCount: 0,
			HasUpvoted: false,
			IsOwn: false);

		// Act
		var dto = CommentProjection.ToDto(row);

		// Assert
		Assert.Equal(expected, dto.Status);
		Assert.Equal("", dto.Content);
		Assert.Equal("", dto.ContentHtml);
	}

	[Theory]
	[InlineData(DataCommentStatus.DeletedByAuthor)]
	[InlineData(DataCommentStatus.HiddenByModerator)]
	public void ToDto_FromEntityThatIsNotVisible_BlanksTheBodies(DataCommentStatus status)
	{
		// Arrange
		var authorId = Guid.NewGuid();
		CommentEntity comment = new()
		{
			Id = Guid.NewGuid(),
			PostId = Guid.NewGuid(),
			AuthorId = authorId,
			Content = "secret",
			ContentHtml = "<p>secret</p>",
			Status = status,
			CreatedOn = Day(1),
			Author = new CommentAuthorEntity { Id = authorId, DisplayName = "Homer" },
		};

		// Act
		var dto = CommentProjection.ToDto(comment, authorId, includeReplies: false);

		// Assert
		Assert.Equal("", dto.Content);
		Assert.Equal("", dto.ContentHtml);
	}

	[Theory]
	[InlineData(DataCommentStatus.HiddenByModerator, "secret", "<p>secret</p>")]
	[InlineData(DataCommentStatus.Visible, "secret", "<p>secret</p>")]
	[InlineData(DataCommentStatus.DeletedByAuthor, "", "")]
	public void ToDto_FromEntityWithModeratedContent_KeepsHiddenTextButNeverDeletedText(
		DataCommentStatus status, string expectedContent, string expectedHtml)
	{
		// Arrange
		var authorId = Guid.NewGuid();
		CommentEntity comment = new()
		{
			Id = Guid.NewGuid(),
			PostId = Guid.NewGuid(),
			AuthorId = authorId,
			Content = "secret",
			ContentHtml = "<p>secret</p>",
			Status = status,
			CreatedOn = Day(1),
			Author = new CommentAuthorEntity { Id = authorId, DisplayName = "Homer" },
		};

		// Act
		var dto = CommentProjection.ToDto(comment, null, includeReplies: false, includeModeratedContent: true);

		// Assert
		Assert.Equal(expectedContent, dto.Content);
		Assert.Equal(expectedHtml, dto.ContentHtml);
	}

	[Fact]
	public void Moq_CanProxyLoggerOfInternalCoreType()
	{
		// Arrange & Act
		var logger = new Mock<ILogger<CommentRow>>();

		// Assert
		Assert.NotNull(logger.Object);
	}

	private static DateTime Day(int day) => new(2026, 8, day, 12, 0, 0, DateTimeKind.Utc);

	private static void SeedPost(DatabaseContext context, Guid postId)
	{
		PostEntity post = new()
		{
			Id = postId,
			Title = "Post",
			RouteName = $"post-{postId:N}",
			LanguageCode = "en",
			Content = "content",
			Abstract = "abstract",
			Status = PostStatus.Published,
			CreatedDate = new DateTimeOffset(2026, 8, 1, 12, 0, 0, TimeSpan.Zero),
			PublishedDate = new DateTimeOffset(2026, 8, 1, 12, 0, 0, TimeSpan.Zero),
		};

		context.Post.Add(post);
	}

	private static void SeedAuthor(DatabaseContext context, Guid authorId, string displayName)
	{
		CommentAuthorEntity author = new()
		{
			Id = authorId,
			DisplayName = displayName,
			Email = $"{displayName.ToLowerInvariant()}@example.com",
			CreatedOn = Day(1),
			LastSeenOn = Day(1),
		};

		context.CommentAuthor.Add(author);
	}

	private static CommentEntity SeedComment(
		DatabaseContext context,
		Guid postId,
		Guid authorId,
		string content,
		DateTime createdOn,
		int upvoteCount = 0,
		int replyCount = 0,
		Guid? parentId = null,
		DataCommentStatus status = DataCommentStatus.Visible)
	{
		CommentEntity comment = new()
		{
			Id = Guid.NewGuid(),
			PostId = postId,
			ParentId = parentId,
			AuthorId = authorId,
			Content = content,
			ContentHtml = $"<p>{content}</p>",
			Status = status,
			CreatedOn = createdOn,
			UpvoteCount = upvoteCount,
			ReplyCount = replyCount,
		};

		context.Comment.Add(comment);
		return comment;
	}
}
