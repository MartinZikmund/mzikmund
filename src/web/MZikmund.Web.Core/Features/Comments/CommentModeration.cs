using Microsoft.EntityFrameworkCore;
using MZikmund.Web.Data;
using MZikmund.Web.Data.Entities;
using DataCommentStatus = MZikmund.Web.Data.Entities.CommentStatus;

namespace MZikmund.Web.Core.Features.Comments;

/// <summary>
/// Steps shared by the moderation handlers. They only stage changes; the caller saves.
/// </summary>
internal static class CommentModeration
{
	/// <summary>
	/// Marks every open report on the comment resolved and zeroes its report counter.
	/// </summary>
	/// <returns>The number of reports that were open.</returns>
	public static async Task<int> ResolveOpenReportsAsync(
		DatabaseContext dbContext,
		CommentEntity comment,
		DateTime resolvedOn,
		CancellationToken cancellationToken)
	{
		var openReports = await dbContext.CommentReport
			.Where(r => r.CommentId == comment.Id && r.ResolvedOn == null)
			.ToListAsync(cancellationToken);

		foreach (var report in openReports)
		{
			report.ResolvedOn = resolvedOn;
		}

		comment.ReportCount = 0;
		return openReports.Count;
	}

	/// <summary>
	/// Sets the comment to hidden. A visible reply stops counting towards its parent's ReplyCount,
	/// mirroring an author delete; hiding something already hidden or deleted leaves the count alone.
	/// </summary>
	public static async Task HideAsync(
		DatabaseContext dbContext,
		CommentEntity comment,
		CancellationToken cancellationToken)
	{
		if (comment.Status == DataCommentStatus.Visible && comment.ParentId is Guid parentId)
		{
			var parent = await dbContext.Comment.FirstOrDefaultAsync(c => c.Id == parentId, cancellationToken);
			if (parent is not null && parent.ReplyCount > 0)
			{
				parent.ReplyCount -= 1;
			}
		}

		comment.Status = DataCommentStatus.HiddenByModerator;
	}
}
