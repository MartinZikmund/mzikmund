namespace MZikmund.DataContracts.Blog.Comments;

/// <summary>
/// Size limits for user-authored comment content. Single source of truth for the API, the web
/// front-end and the Uno app.
/// </summary>
public static class CommentLimits
{
	/// <summary>
	/// Maximum length of a comment body, in characters.
	/// </summary>
	public const int MaxCommentLength = 4000;

	/// <summary>
	/// Maximum length of the optional note attached to a comment report, in characters.
	/// </summary>
	public const int MaxReportNoteLength = 500;

	/// <summary>
	/// Top-level comments per page, shared by the API and the server-rendered first page.
	/// </summary>
	public const int PageSize = 20;

	/// <summary>
	/// Replies per page when a thread's overflow is expanded.
	/// </summary>
	public const int RepliesPageSize = 20;

	/// <summary>
	/// Minutes after posting during which an author can still edit a comment.
	/// </summary>
	public const int EditWindowMinutes = 15;

	/// <summary>
	/// How long after posting an author can still edit a comment.
	/// </summary>
	public static readonly TimeSpan EditWindow = TimeSpan.FromMinutes(EditWindowMinutes);
}
