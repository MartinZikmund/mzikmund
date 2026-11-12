namespace MZikmund.DataContracts.Blog.Comments;

/// <summary>
/// The signed-in reader's public comment identity.
/// </summary>
public record CommentProfile
{
	/// <summary>
	/// Name shown on the reader's comments (or the one the next comment would get).
	/// </summary>
	public required string DisplayName { get; init; }

	/// <summary>
	/// Whether the reader picked the name themselves rather than it coming from their sign-in provider.
	/// </summary>
	public required bool HasChosenDisplayName { get; init; }

	/// <summary>
	/// Whether the reader is banned from commenting.
	/// </summary>
	public required bool IsBanned { get; init; }
}
