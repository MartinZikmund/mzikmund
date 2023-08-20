using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MZikmund.Web.Core.Dtos;
using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Infrastructure;

namespace MZikmund.Web.Core.Blog;

public class GetCategoryByRouteNameHandler : IRequestHandler<GetCategoryByRouteNameQuery, Category>
{
	private readonly IRepository<CategoryEntity> _categoriesRepository;
	private readonly IMapper _mapper;

	public GetCategoryByRouteNameHandler(
		IRepository<CategoryEntity> categoriesRepository,
		IMapper mapper)
	{
		_categoriesRepository = categoriesRepository;
		_mapper = mapper;
	}

	public async Task<Category> Handle(GetCategoryByRouteNameQuery request, CancellationToken cancellationToken)
	{
		var post = await _categoriesRepository.AsQueryable()
			.Where(p => p.RouteName.Equals(request.RouteName))
			.FirstOrDefaultAsync();
		return _mapper.Map<Category>(post);
	}
}
