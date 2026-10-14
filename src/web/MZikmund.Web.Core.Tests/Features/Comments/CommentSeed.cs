using Microsoft.Extensions.Logging;
using Moq;
using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Web.Core.Services;
using MZikmund.Web.Core.Services.Comments;
using MZikmund.Web.Data;
using MZikmund.Web.Data.Entities;
using DataCommentStatus = MZikmund.Web.Data.Entities.CommentStatus;

namespace MZikmund.Web.Core.Tests.Features.Comments;

/// <summary>
/// Row builders and mock factories shared by the comment handler tests.
/// </summary>
internal static class CommentSeed
{
	public static readonly DateTimeOffset Now = new(2026, 8, 11, 10, 0, 0, TimeSpan.Zero);

	public static DateTime Day(int day) => new(2026, 8, day, 12, 0, 0, DateTimeKind.Utc);

	public static PostEntity AddPost(
		DatabaseContext context,
		Guid postId,
		PostStatus status = PostStatus.Published,
		DateTimeOffset? publishedDate = null)
	{
		PostEntity post = new()
		{
			Id = postId,
			Title = "Post",
			RouteName = $"post-{postId:N}",
			LanguageCode = "en-US",
			Content = "content",
			Abstract = "abstract",
			Status = status,
			CreatedDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
			PublishedDate = publishedDate ?? new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
		};

		context.Post.Add(post);
		return post;
	}

	public static CommentAuthorEntity AddAuthor(DatabaseContext context, Guid authorId, string displayName)
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
		return author;
	}

	public static CommentEntity AddComment(
		DatabaseContext context,
		Guid postId,
		Guid authorId,
		string content = "Existing comment",
		DateTime? createdOn = null,
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
			CreatedOn = createdOn ?? Now.UtcDateTime,
			UpvoteCount = upvoteCount,
			ReplyCount = replyCount,
		};

		context.Comment.Add(comment);
		return comment;
	}

	public static CommentReportEntity AddReport(
		DatabaseContext context,
		Guid commentId,
		Guid reporterId,
		CommentReportReason reason,
		DateTime createdOn)
	{
		CommentReportEntity report = new()
		{
			Id = Guid.NewGuid(),
			CommentId = commentId,
			ReporterId = reporterId,
			Reason = reason,
			CreatedOn = createdOn,
		};

		context.CommentReport.Add(report);
		return report;
	}

	public static Mock<ICurrentUserAccessor> CurrentUser(Guid? authorId, string displayName = "Reader")
	{
		Mock<ICurrentUserAccessor> accessor = new();
		CommentAuthorInfo? info = authorId is null
			? null
			: new CommentAuthorInfo(authorId.Value, displayName, null, $"{displayName.ToLowerInvariant()}@example.com", false);
		accessor.Setup(a => a.Current).Returns(info);
		return accessor;
	}

	public static Mock<IDateProvider> FixedClock()
	{
		Mock<IDateProvider> clock = new();
		clock.Setup(d => d.UtcNow).Returns(Now);
		clock.Setup(d => d.Now).Returns(Now);
		return clock;
	}

	public static Mock<ILogger<T>> Logger<T>() => new();
}
