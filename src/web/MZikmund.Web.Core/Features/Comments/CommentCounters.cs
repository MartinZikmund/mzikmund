using Microsoft.EntityFrameworkCore;
using MZikmund.Web.Data;

namespace MZikmund.Web.Core.Features.Comments;

/// <summary>
/// Atomic updates of the denormalized counters. Each is a single UPDATE evaluated by the database,
/// so concurrent writers cannot overwrite each other's increment the way a read-modify-write would.
/// Callers run them inside the same transaction as the row change they mirror.
/// </summary>
internal static class CommentCounters
{
	public static Task<int> IncrementReplyCountAsync(DatabaseContext dbContext, Guid commentId, CancellationToken cancellationToken) =>
		dbContext.Comment
			.Where(c => c.Id == commentId)
			.ExecuteUpdateAsync(s => s.SetProperty(c => c.ReplyCount, c => c.ReplyCount + 1), cancellationToken);

	public static Task<int> DecrementReplyCountAsync(DatabaseContext dbContext, Guid commentId, CancellationToken cancellationToken) =>
		dbContext.Comment
			.Where(c => c.Id == commentId && c.ReplyCount > 0)
			.ExecuteUpdateAsync(s => s.SetProperty(c => c.ReplyCount, c => c.ReplyCount - 1), cancellationToken);

	public static Task<int> IncrementUpvoteCountAsync(DatabaseContext dbContext, Guid commentId, CancellationToken cancellationToken) =>
		dbContext.Comment
			.Where(c => c.Id == commentId)
			.ExecuteUpdateAsync(s => s.SetProperty(c => c.UpvoteCount, c => c.UpvoteCount + 1), cancellationToken);

	public static Task<int> DecrementUpvoteCountAsync(DatabaseContext dbContext, Guid commentId, CancellationToken cancellationToken) =>
		dbContext.Comment
			.Where(c => c.Id == commentId && c.UpvoteCount > 0)
			.ExecuteUpdateAsync(s => s.SetProperty(c => c.UpvoteCount, c => c.UpvoteCount - 1), cancellationToken);

	public static Task<int> IncrementReportCountAsync(DatabaseContext dbContext, Guid commentId, CancellationToken cancellationToken) =>
		dbContext.Comment
			.Where(c => c.Id == commentId)
			.ExecuteUpdateAsync(s => s.SetProperty(c => c.ReportCount, c => c.ReportCount + 1), cancellationToken);
}
