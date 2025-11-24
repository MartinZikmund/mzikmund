using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Infrastructure;

namespace MZikmund.Web.Data.Specifications.Posts;

public sealed class GetPostsSpecification : BaseSpecification<PostEntity>
{
	public GetPostsSpecification(int pageNumber, int pageSize, Guid? categoryId = null, Guid? tagId = null, string? searchTerm = null)
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

		if (!string.IsNullOrWhiteSpace(searchTerm))
		{
			AddCriteria(p => p.Title.Contains(searchTerm) || p.Content.Contains(searchTerm) || p.Abstract.Contains(searchTerm));
		}

		AddInclude(p => p.Include(post => post.Tags));
		AddInclude(p => p.Include(post => post.Categories));
		ApplyPaging(startRow, pageSize);
		ApplyOrderByDescending(p => p.PublishedDate);
	}
}
