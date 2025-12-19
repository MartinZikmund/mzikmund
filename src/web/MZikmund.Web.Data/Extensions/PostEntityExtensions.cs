using System.Linq.Expressions;
using MZikmund.Web.Data.Entities;

namespace MZikmund.Web.Data.Extensions;

/// <summary>
/// Extension methods for <see cref="PostEntity"/> queries.
/// </summary>
public static class PostEntityExtensions
{
	/// <summary>
	/// Gets an expression that filters posts to only include published posts
	/// with a published date in the past or present.
	/// </summary>
	/// <param name="now">The current date and time to compare against.</param>
	/// <returns>An expression that can be used in LINQ queries.</returns>
	public static Expression<Func<PostEntity, bool>> IsPublishedAndVisible(DateTimeOffset now) =>
		p => p.Status == PostStatus.Published && p.PublishedDate != null && p.PublishedDate <= now;
}
