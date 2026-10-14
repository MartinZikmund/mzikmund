namespace MZikmund.DataContracts.Blog.Comments;

/// <summary>
/// Body of a request that posts a new comment or reply.
/// </summary>
public record CreateCommentRequest
{
	/// <summary>
	/// Raw markdown body of the comment.
	/// </summary>
	public required string Content { get; init; }

	/// <summary>
	/// Identifier of the comment being replied to, or <c>null</c> for a top-level comment.
	/// </summary>
	public Guid? ParentId { get; init; }
}
