using AutoMapper;
using MediatR;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Infrastructure;

namespace MZikmund.Web.Core.Blog;

internal class UpdateCategoryHandler : IRequestHandler<UpdateCategoryCommand, Category>
{
	private readonly IRepository<CategoryEntity> _categoriesRepository;
	private readonly IMapper _mapper;

	public UpdateCategoryHandler(IRepository<CategoryEntity> categoriesRepository, IMapper mapper)
	{
		_categoriesRepository = categoriesRepository;
		_mapper = mapper;
	}

	public async Task<Category> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
	{
		var updatedCategory = request.UpdatedCategory;
		var category = await _categoriesRepository.GetAsync(request.CategoryId, cancellationToken);
		if (category is null)
		{
			throw new KeyNotFoundException("Category not found"); // TODO: Use a more descriptive exception.
		}

		category.DisplayName = updatedCategory.DisplayName;
		category.RouteName = updatedCategory.RouteName;
		category.Description = updatedCategory.Description;
		category.Icon = updatedCategory.Icon;

		await _categoriesRepository.UpdateAsync(category, cancellationToken);

		return _mapper.Map<Category>(category);
	}
}
