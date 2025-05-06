using AutoMapper;
using MediatR;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Infrastructure;

namespace MZikmund.Web.Core.Features.Categories;

public class GetCategoriesHandler : IRequestHandler<GetCategoriesQuery, IReadOnlyList<Category>>
{
	private readonly IRepository<CategoryEntity> _categoryRepository;
	private readonly IMapper _mapper;

	public GetCategoriesHandler(
		IRepository<CategoryEntity> categoryRepository,
		IMapper mapper)
	{
		_categoryRepository = categoryRepository;
		_mapper = mapper;
	}

	public async Task<IReadOnlyList<Category>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
	{
		var categories = await _categoryRepository.ListAsync(cancellationToken);
		return _mapper.Map<IReadOnlyList<Category>>(categories);
	}
}
