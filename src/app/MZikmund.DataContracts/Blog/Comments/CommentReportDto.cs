namespace MZikmund.DataContracts.Blog.Comments;

/// <summary>
/// One entry of the moderation queue: a comment together with its open reports.
/// </summary>
public record CommentReportDto
{
	/// <summary>
	/// Identifier of the reported comment.
	/// </summary>
	public required Guid CommentId { get; init; }

	/// <summary>
	/// Identifier of the comment author, so the queue can ban directly.
	/// </summary>
	public required Guid AuthorId { get; init; }

	/// <summary>
	/// The reported comment itself.
	/// </summary>
	public required CommentDto Comment { get; init; }

	/// <summary>
	/// Number of unresolved reports filed against the comment.
	/// </summary>
	public required int OpenReportCount { get; init; }

	/// <summary>
	/// Distinct reasons given across the open reports.
	/// </summary>
	public required CommentReportReason[] Reasons { get; init; }

	/// <summary>
	/// UTC timestamp of the earliest open report, used to order the queue.
	/// </summary>
	public required DateTime FirstReportedOn { get; init; }
}
