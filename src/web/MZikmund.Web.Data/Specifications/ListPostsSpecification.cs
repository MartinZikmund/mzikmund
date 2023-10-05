using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Infrastructure;

namespace MZikmund.Web.Data.Specifications;

public sealed class ListPostsSpecification : BaseSpecification<PostEntity>
{
	public ListPostsSpecification(int pageNumber, int pageSize, Guid? categoryId = null, Guid? tagId = null)
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
