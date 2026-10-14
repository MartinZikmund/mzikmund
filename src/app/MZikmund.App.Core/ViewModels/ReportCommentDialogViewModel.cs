using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Services.Localization;

namespace MZikmund.ViewModels;

public class ReportCommentDialogViewModel : DialogViewModel
{
	private static readonly CommentReportReason[] Reasons =
	[
		CommentReportReason.Spam,
		CommentReportReason.Abuse,
		CommentReportReason.OffTopic,
		CommentReportReason.Other,
	];

	public override string Title => Localizer.Instance.GetString("ReportCommentTitle");

	public int SelectedReasonIndex { get; set; }

	public string Note { get; set; } = "";

	public int MaxNoteLength => CommentLimits.MaxReportNoteLength;

	public CommentReportReason Reason =>
		SelectedReasonIndex >= 0 && SelectedReasonIndex < Reasons.Length ? Reasons[SelectedReasonIndex] : CommentReportReason.Other;

	public string? NoteOrNull => string.IsNullOrWhiteSpace(Note) ? null : Note.Trim();
}
