namespace MZikmund.DataContracts.Blog.Comments;

/// <summary>
/// Reason a reader gave when reporting a comment.
/// </summary>
public enum CommentReportReason
{
	/// <summary>
	/// Unsolicited advertising or link spam.
	/// </summary>
	Spam,

	/// <summary>
	/// Harassment, hate speech or personal attacks.
	/// </summary>
	Abuse,

	/// <summary>
	/// Unrelated to the post being commented on.
	/// </summary>
	OffTopic,

	/// <summary>
	/// Anything else; the optional note carries the detail.
	/// </summary>
	Other,
}
