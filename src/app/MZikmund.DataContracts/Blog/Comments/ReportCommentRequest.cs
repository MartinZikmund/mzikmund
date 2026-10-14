namespace MZikmund.DataContracts.Blog.Comments;

/// <summary>
/// Body of a request that reports a comment to the moderator.
/// </summary>
public record ReportCommentRequest
{
	/// <summary>
	/// Reason the reader is reporting the comment.
	/// </summary>
	public required CommentReportReason Reason { get; init; }

	/// <summary>
	/// Optional free-text detail, capped at 500 characters by the server.
	/// </summary>
	public string? Note { get; init; }
}
