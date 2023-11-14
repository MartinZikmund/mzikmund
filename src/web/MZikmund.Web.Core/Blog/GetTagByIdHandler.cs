using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Infrastructure;

namespace MZikmund.Web.Core.Blog;

public class GetTagByIdHandler : IRequestHandler<GetTagByIdQuery, Tag>
{
	private readonly IRepository<TagEntity> _tagsRepository;
	private readonly IMapper _mapper;

	public GetTagByIdHandler(
		IRepository<TagEntity> tagsRepository,
		IMapper mapper)
	{
		_tagsRepository = tagsRepository;
		_mapper = mapper;
	}

	public async Task<Tag> Handle(GetTagByIdQuery request, CancellationToken cancellationToken)
	{
		var tag = await _tagsRepository.GetAsync(request.TagId);
		return _mapper.Map<Tag>(tag);
	}
}
