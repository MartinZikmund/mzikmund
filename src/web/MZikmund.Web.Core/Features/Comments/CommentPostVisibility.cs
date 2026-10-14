using Microsoft.EntityFrameworkCore;
using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Extensions;

namespace MZikmund.Web.Core.Features.Comments;

internal static class CommentPostVisibility
{
	/// <summary>
	/// Whether the post is published and not future-dated, using the same rule as the post
	/// handlers. Evaluated in memory because not every provider translates
	/// <see cref="DateTimeOffset"/> comparisons.
	/// </summary>
	public static async Task<bool> IsPublicAsync(
		IQueryable<PostEntity> posts,
		Guid postId,
		CancellationToken cancellationToken)
	{
		var post = await posts
			.AsNoTracking()
			.Where(p => p.Id == postId)
			.Select(p => new { p.Status, p.PublishedDate })
			.FirstOrDefaultAsync(cancellationToken);

		if (post is null)
		{
			return false;
		}

		PostEntity probe = new() { Status = post.Status, PublishedDate = post.PublishedDate };
		return PostEntityExtensions.IsPublishedAndVisible(DateTimeOffset.UtcNow).Compile()(probe);
	}
}
