using AutoMapper;
using MediatR;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Infrastructure;

namespace MZikmund.Web.Core.Features.Tags;

public class GetTagsHandler : IRequestHandler<GetTagsQuery, IReadOnlyList<Tag>>
{
	private readonly IRepository<TagEntity> _tagRepository;
	private readonly IMapper _mapper;

	public GetTagsHandler(
		IRepository<TagEntity> tagRepository,
		IMapper mapper)
	{
		_tagRepository = tagRepository;
		_mapper = mapper;
	}

	public async Task<IReadOnlyList<Tag>> Handle(GetTagsQuery request, CancellationToken cancellationToken)
	{
		var tags = await _tagRepository.ListAsync(cancellationToken);
		return _mapper.Map<IReadOnlyList<Tag>>(tags);
	}
}
