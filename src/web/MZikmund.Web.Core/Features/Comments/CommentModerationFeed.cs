using MZikmund.Web.Data;
using MZikmund.Web.Data.Entities;

namespace MZikmund.Web.Core.Features.Comments;

internal static class CommentModerationFeed
{
	/// <summary>
	/// Every comment regardless of status, newer than <paramref name="since"/> when given. The feed
	/// and the new-comment badge share it, so they can never disagree.
	/// </summary>
	public static IQueryable<CommentEntity> Since(DatabaseContext dbContext, DateTime? since) =>
		dbContext.Comment.Where(c => since == null || c.CreatedOn > since);
}
