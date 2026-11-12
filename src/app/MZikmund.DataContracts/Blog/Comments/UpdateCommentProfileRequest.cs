namespace MZikmund.DataContracts.Blog.Comments;

/// <summary>
/// Body of a request that sets the reader's comment display name.
/// </summary>
public record UpdateCommentProfileRequest
{
	/// <summary>
	/// Wanted display name. The server normalizes and validates it.
	/// </summary>
	public required string DisplayName { get; init; }
}
