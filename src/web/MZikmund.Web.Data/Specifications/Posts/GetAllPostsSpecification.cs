using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Infrastructure;

namespace MZikmund.Web.Data.Specifications.Posts;

/// <summary>
/// Specification for getting all posts without filtering by published status (for admin use).
/// </summary>
public sealed class GetAllPostsSpecification : BaseSpecification<PostEntity>
{
	public GetAllPostsSpecification(int pageNumber, int pageSize, Guid? categoryId = null, Guid? tagId = null)
	{
		var startRow = (pageNumber - 1) * pageSize;

		if (categoryId is not null)
		{
			AddCriteria(p => p.Categories.Any(c => c.Id == categoryId));
		}

		if (tagId is not null)
		{
			AddCriteria(p => p.Tags.Any(t => t.Id == tagId));
		}

		AddInclude(p => p.Include(post => post.Tags));
		AddInclude(p => p.Include(post => post.Categories));
		ApplyPaging(startRow, pageSize);
		ApplyOrderByDescending(p => p.PublishedDate);
	}
}
