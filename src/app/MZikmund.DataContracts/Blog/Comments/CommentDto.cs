namespace MZikmund.DataContracts.Blog.Comments;

/// <summary>
/// Represents a single comment on a blog post as returned by the public API.
/// </summary>
public record CommentDto
{
	/// <summary>
	/// Unique identifier of the comment.
	/// </summary>
	public required Guid Id { get; init; }

	/// <summary>
	/// Identifier of the post the comment belongs to.
	/// </summary>
	public required Guid PostId { get; init; }

	/// <summary>
	/// Identifier of the parent comment, or <c>null</c> for a top-level comment.
	/// </summary>
	public Guid? ParentId { get; init; }

	/// <summary>
	/// Display name of the author at the time the comment was read.
	/// </summary>
	public required string AuthorDisplayName { get; init; }

	/// <summary>
	/// Absolute URL of the author's avatar, when the identity provider supplied one.
	/// </summary>
	public string? AuthorAvatarUrl { get; init; }

	/// <summary>
	/// Display name of the comment this reply was originally aimed at, when the reply was
	/// re-pointed to its grandparent to keep the thread two levels deep.
	/// </summary>
	public string? MentionedDisplayName { get; init; }

	/// <summary>
	/// Raw markdown exactly as the author typed it, used by the native app.
	/// </summary>
	public required string Content { get; init; }

	/// <summary>
	/// Rendered and sanitized HTML, used by the web front-end.
	/// </summary>
	public required string ContentHtml { get; init; }

	/// <summary>
	/// Visibility status of the comment.
	/// </summary>
	public required CommentStatus Status { get; init; }

	/// <summary>
	/// UTC timestamp of when the comment was posted.
	/// </summary>
	public required DateTime CreatedOn { get; init; }

	/// <summary>
	/// UTC timestamp of the last edit, or <c>null</c> when the comment was never edited.
	/// </summary>
	public DateTime? EditedOn { get; init; }

	/// <summary>
	/// Number of upvotes the comment has received.
	/// </summary>
	public required int UpvoteCount { get; init; }

	/// <summary>
	/// Total number of replies, which may exceed the length of <see cref="Replies"/>.
	/// </summary>
	public required int ReplyCount { get; init; }

	/// <summary>
	/// Whether the caller has upvoted this comment.
	/// </summary>
	public required bool HasUpvoted { get; init; }

	/// <summary>
	/// Whether the caller is the author of this comment.
	/// </summary>
	public required bool IsOwn { get; init; }

	/// <summary>
	/// First three replies ordered by <see cref="CreatedOn"/> ascending. Empty on a reply.
	/// </summary>
	public CommentDto[] Replies { get; init; } = Array.Empty<CommentDto>();
}
