using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Infrastructure;

namespace MZikmund.Web.Core.Features.Categories;

public class GetCategoryByIdHandler : IRequestHandler<GetCategoryByIdQuery, Category>
{
	private readonly IRepository<CategoryEntity> _categoriesRepository;
	private readonly IMapper _mapper;

	public GetCategoryByIdHandler(
		IRepository<CategoryEntity> categoriesRepository,
		IMapper mapper)
	{
		_categoriesRepository = categoriesRepository;
		_mapper = mapper;
	}

	public async Task<Category> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
	{
		var category = await _categoriesRepository.GetAsync(request.CategoryId);
		return _mapper.Map<Category>(category);
	}
}
