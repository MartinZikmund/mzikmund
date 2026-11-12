namespace MZikmund.DataContracts.Blog.Comments;

/// <summary>
/// Body of a request that edits an existing comment.
/// </summary>
public record UpdateCommentRequest
{
	/// <summary>
	/// New raw markdown body of the comment.
	/// </summary>
	public required string Content { get; init; }
}
