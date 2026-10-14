using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using MZikmund.Web.Data.Entities;

namespace MZikmund.Web.Core.Features.Comments;

internal static class CommentPostVisibility
{
	// Compiled once. Same rule as PostEntityExtensions.IsPublishedAndVisible, which cannot be
	// compiled once because it closes over "now"; a test keeps the two from drifting apart.
	internal static readonly Expression<Func<PostEntity, DateTimeOffset, bool>> PublicRule =
		(post, now) => post.Status == PostStatus.Published && post.PublishedDate != null && post.PublishedDate <= now;

	private static readonly Func<PostEntity, DateTimeOffset, bool> IsPublicPredicate = PublicRule.Compile();

	/// <summary>
	/// Whether the post is published and not future-dated, using the same rule as the post
	/// handlers. Evaluated in memory because not every provider translates
	/// <see cref="DateTimeOffset"/> comparisons.
	/// </summary>
	public static async Task<bool> IsPublicAsync(
		IQueryable<PostEntity> posts,
		Guid postId,
		DateTimeOffset now,
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
		return IsPublicPredicate(probe, now);
	}
}
