namespace MZikmund.DataContracts.Blog.Comments;

/// <summary>
/// Ordering applied to the top-level comments of a post.
/// </summary>
public enum CommentSortOrder
{
	/// <summary>
	/// Most upvoted first, ties broken by newest first.
	/// </summary>
	Top,

	/// <summary>
	/// Newest first.
	/// </summary>
	New,
}
