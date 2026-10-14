namespace MZikmund.DataContracts.Blog.Comments;

/// <summary>
/// Stable machine-readable reasons carried in the <c>code</c> extension of a comment ProblemDetails,
/// so clients can show their own localized text instead of the server's English message.
/// </summary>
public static class CommentErrorCodes
{
	public const string NotFound = "not_found";
	public const string Forbidden = "forbidden";
	public const string Validation = "validation";
	public const string Conflict = "conflict";

	public const string Banned = "banned";
	public const string ContentEmpty = "content_empty";
	public const string ContentTooLong = "content_too_long";
	public const string EditWindowExpired = "edit_window_expired";
	public const string NotVisible = "not_visible";
	public const string OwnComment = "own_comment";
	public const string HiddenByModerator = "hidden_by_moderator";
}
