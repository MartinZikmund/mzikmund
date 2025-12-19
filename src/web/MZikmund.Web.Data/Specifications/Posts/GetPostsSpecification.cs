using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Infrastructure;

namespace MZikmund.Web.Data.Specifications.Posts;

public sealed class GetPostsSpecification : BaseSpecification<PostEntity>
{
	public GetPostsSpecification(int pageNumber, int pageSize, Guid? categoryId = null, Guid? tagId = null)
	{
		var startRow = (pageNumber - 1) * pageSize;
		var now = DateTimeOffset.UtcNow;

		// Only show published posts with published date in the past or present
		AddCriteria(p => p.Status == PostStatus.Published);
		AddCriteria(p => p.PublishedDate != null && p.PublishedDate <= now);

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
