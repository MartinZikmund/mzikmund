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
}
