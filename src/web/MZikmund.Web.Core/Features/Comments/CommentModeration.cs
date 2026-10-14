using Microsoft.EntityFrameworkCore;
using MZikmund.Web.Core.Services.Comments;
using MZikmund.Web.Data;
using DataCommentStatus = MZikmund.Web.Data.Entities.CommentStatus;

namespace MZikmund.Web.Core.Features.Comments;

/// <summary>
/// Steps shared by the moderation handlers. They write straight to the database, so callers run
/// them inside one transaction.
/// </summary>
internal static class CommentModeration
{
	/// <summary>
	/// Marks every open report on the comment resolved and zeroes its report counter.
	/// </summary>
	/// <returns>The number of reports that were open.</returns>
	public static async Task<int> ResolveOpenReportsAsync(
		DatabaseContext dbContext,
		Guid commentId,
		DateTime resolvedOn,
		CancellationToken cancellationToken)
	{
		var resolved = await dbContext.CommentReport
			.Where(r => r.CommentId == commentId && r.ResolvedOn == null)
			.ExecuteUpdateAsync(s => s.SetProperty(r => r.ResolvedOn, (DateTime?)resolvedOn), cancellationToken);

		await dbContext.Comment
			.Where(c => c.Id == commentId)
			.ExecuteUpdateAsync(s => s.SetProperty(c => c.ReportCount, 0), cancellationToken);

		return resolved;
	}

	/// <summary>
	/// Sets the comment to hidden. A visible reply stops counting towards its parent's ReplyCount,
	/// mirroring an author delete; hiding something already hidden or deleted leaves the count alone.
	/// </summary>
	/// <param name="purgeContent">Whether to also erase the text, as a moderator delete does.</param>
	public static async Task HideAsync(
		DatabaseContext dbContext,
		Guid commentId,
		Guid? parentId,
		bool purgeContent,
		CancellationToken cancellationToken)
	{
		// The status filter makes the transition itself the guard: only the writer that actually
		// flips Visible to Hidden decrements the parent, so a double hide cannot count twice.
		var hiddenFromVisible = await dbContext.Comment
			.Where(c => c.Id == commentId && c.Status == DataCommentStatus.Visible)
			.ExecuteUpdateAsync(s => s.SetProperty(c => c.Status, DataCommentStatus.HiddenByModerator), cancellationToken);

		await dbContext.Comment
			.Where(c => c.Id == commentId && c.Status == DataCommentStatus.DeletedByAuthor)
			.ExecuteUpdateAsync(s => s.SetProperty(c => c.Status, DataCommentStatus.HiddenByModerator), cancellationToken);

		if (purgeContent)
		{
			await dbContext.Comment
				.Where(c => c.Id == commentId)
				.ExecuteUpdateAsync(
					s => s.SetProperty(c => c.Content, "").SetProperty(c => c.ContentHtml, ""),
					cancellationToken);
		}

		if (hiddenFromVisible > 0 && parentId is Guid parent)
		{
			await CommentCounters.DecrementReplyCountAsync(dbContext, parent, cancellationToken);
		}
	}

	/// <summary>
	/// Returns the parent id of the comment (null for a top-level one), or throws when it does not exist.
	/// </summary>
	public static async Task<Guid?> FindParentIdAsync(
		DatabaseContext dbContext,
		Guid commentId,
		CancellationToken cancellationToken)
	{
		var found = await dbContext.Comment
			.AsNoTracking()
			.Where(c => c.Id == commentId)
			.Select(c => new { c.ParentId })
			.FirstOrDefaultAsync(cancellationToken);

		return found is null
			? throw new CommentNotFoundException($"Comment {commentId} was not found.")
			: found.ParentId;
	}
}
