namespace MZikmund.DataContracts.Blog.Comments;

/// <summary>
/// Visibility status of a comment.
/// </summary>
public enum CommentStatus
{
	/// <summary>
	/// The comment is visible with its content.
	/// </summary>
	Visible,

	/// <summary>
	/// The author deleted the comment; a tombstone is shown when it still has replies.
	/// </summary>
	DeletedByAuthor,

	/// <summary>
	/// A moderator hid the comment.
	/// </summary>
	HiddenByModerator,
}
