using MZikmund.DataContracts.Blog.Comments;

namespace MZikmund.Web.Data.Entities;

public class CommentReportEntity
{
	public Guid Id { get; set; }

	public Guid CommentId { get; set; }

	public Guid ReporterId { get; set; }

	public CommentReportReason Reason { get; set; }

	public string? Note { get; set; }

	public DateTime CreatedOn { get; set; }

	public DateTime? ResolvedOn { get; set; }
}
