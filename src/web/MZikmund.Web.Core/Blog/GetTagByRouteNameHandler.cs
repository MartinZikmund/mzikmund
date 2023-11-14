using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Infrastructure;

namespace MZikmund.Web.Core.Blog;

public class GetTagByRouteNameHandler : IRequestHandler<GetTagByRouteNameQuery, Tag>
{
	private readonly IRepository<TagEntity> _tagsRepository;
	private readonly IMapper _mapper;

	public GetTagByRouteNameHandler(
		IRepository<TagEntity> tagsRepository,
		IMapper mapper)
	{
		_tagsRepository = tagsRepository;
		_mapper = mapper;
	}

	public async Task<Tag> Handle(GetTagByRouteNameQuery request, CancellationToken cancellationToken)
	{
		var post = await _tagsRepository.AsQueryable()
			.Where(p => p.RouteName.Equals(request.RouteName))
			.FirstOrDefaultAsync();
		return _mapper.Map<Tag>(post);
	}
}
