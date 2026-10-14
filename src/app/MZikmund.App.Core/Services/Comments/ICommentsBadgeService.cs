namespace MZikmund.Services.Comments;

/// <summary>
/// Tracks how many comments arrived since the moderator last opened the comments page.
/// </summary>
public interface ICommentsBadgeService
{
	int UnreadCount { get; }

	/// <summary>
	/// Raised when <see cref="UnreadCount"/> changes, possibly off the UI thread.
	/// </summary>
	event EventHandler? Changed;

	/// <summary>
	/// Asks the server for the count. Failures are logged and swallowed: a badge never shows an error.
	/// </summary>
	Task RefreshAsync();

	/// <summary>
	/// Records that the moderator has now seen everything and clears the count.
	/// </summary>
	void MarkSeen();
}
