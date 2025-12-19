using MediatR;
using Microsoft.EntityFrameworkCore;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Extensions;
using MZikmund.Web.Data.Infrastructure;

namespace MZikmund.Web.Core.Features.Categories;

public class GetCategoriesWithPostCountHandler : IRequestHandler<GetCategoriesWithPostCountQuery, IReadOnlyList<CategoryWithPostCount>>
{
	private readonly IRepository<CategoryEntity> _categoryRepository;

	public GetCategoriesWithPostCountHandler(
		IRepository<CategoryEntity> categoryRepository)
	{
		_categoryRepository = categoryRepository;
	}

	public async Task<IReadOnlyList<CategoryWithPostCount>> Handle(GetCategoriesWithPostCountQuery request, CancellationToken cancellationToken)
	{
		var now = DateTimeOffset.UtcNow;
		var categories = await _categoryRepository.AsQueryable()
			.Select(c => new CategoryWithPostCount
			{
				Id = c.Id,
				DisplayName = c.DisplayName,
				Description = c.Description,
				Icon = c.Icon,
				RouteName = c.RouteName,
				PostCount = c.Posts.AsQueryable().Count(PostEntityExtensions.IsPublishedAndVisible(now))
			})
			.OrderBy(c => c.DisplayName)
			.ToListAsync(cancellationToken);

		return categories;
	}
}
