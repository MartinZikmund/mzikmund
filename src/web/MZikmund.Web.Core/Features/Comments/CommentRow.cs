using MZikmund.DataContracts.Blog.Comments;
using DataCommentStatus = MZikmund.Web.Data.Entities.CommentStatus;

namespace MZikmund.Web.Core.Features.Comments;

/// <summary>
/// Flat row a comment is read into before it becomes a <see cref="CommentDto"/>. Keeping the
/// database projection primitive keeps it translatable on every provider.
/// </summary>
internal sealed record CommentRow(
	Guid Id,
	Guid PostId,
	Guid? ParentId,
	string AuthorDisplayName,
	string? AuthorAvatarUrl,
	string? MentionedDisplayName,
	string Content,
	string ContentHtml,
	DataCommentStatus Status,
	DateTime CreatedOn,
	DateTime? EditedOn,
	int UpvoteCount,
	int ReplyCount,
	bool HasUpvoted,
	bool IsOwn);
