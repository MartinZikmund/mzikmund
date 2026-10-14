using Humanizer;
using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Services.Localization;

namespace MZikmund.ViewModels.Admin;

/// <summary>
/// One row of the moderation page: either a queue entry (with its open reports) or a plain feed entry.
/// </summary>
public class CommentReportItemViewModel
{
	public CommentReportItemViewModel(CommentReportDto report)
	{
		Comment = report.Comment;
		AuthorId = report.AuthorId;
		Report = report;
	}

	public CommentReportItemViewModel(CommentModerationDto comment)
	{
		Comment = comment;
		AuthorId = comment.AuthorId;
	}

	public CommentDto Comment { get; }

	public Guid AuthorId { get; }

	public CommentReportDto? Report { get; }

	public Guid Id => Comment.Id;

	public bool IsReport => Report is not null;

	public string DisplayName => Comment.AuthorDisplayName;

	// Moderators get the text of hidden comments; a comment its author deleted arrives blank.
	public string Content => Comment.Content;

	public bool HasContent => !string.IsNullOrWhiteSpace(Comment.Content);

	public bool CanHide => Comment.Status == CommentStatus.Visible;

	public bool IsNotVisible => Comment.Status != CommentStatus.Visible;

	public string StatusText => Localizer.Instance.GetString(
		Comment.Status == CommentStatus.HiddenByModerator ? "ModeratorStatusHidden" : "ModeratorStatusDeleted");

	public int OpenReportCount => Report?.OpenReportCount ?? 0;

	public string ReportCountText => string.Format(
		System.Globalization.CultureInfo.CurrentCulture,
		Localizer.Instance.GetString("ModeratorReportCountFormat"),
		OpenReportCount);

	public string MetaText => Report is { } report ?
		string.Format(
			System.Globalization.CultureInfo.CurrentCulture,
			Localizer.Instance.GetString("ModeratorReportMetaFormat"),
			string.Join(", ", report.Reasons.Select(ReasonText)),
			report.FirstReportedOn.Humanize()) :
		Comment.CreatedOn.Humanize();

	private static string ReasonText(CommentReportReason reason) => Localizer.Instance.GetString($"ReportReason{reason}");
}
