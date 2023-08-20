using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MZikmund.Web.Core.Dtos;
using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Infrastructure;

namespace MZikmund.Web.Core.Blog;

public class GetTagByRouteNameHandler : IRequestHandler<GetTagByRouteNameQuery, Tag>
{
	private readonly IRepository<TagEntity> _categoriesRepository;
	private readonly IMapper _mapper;

	public GetTagByRouteNameHandler(
		IRepository<TagEntity> categoriesRepository,
		IMapper mapper)
	{
		_categoriesRepository = categoriesRepository;
		_mapper = mapper;
	}

	public async Task<Tag> Handle(GetTagByRouteNameQuery request, CancellationToken cancellationToken)
	{
		var post = await _categoriesRepository.AsQueryable()
			.Where(p => p.RouteName.Equals(request.RouteName))
			.FirstOrDefaultAsync();
		return _mapper.Map<Tag>(post);
	}
}
