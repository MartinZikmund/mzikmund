using AutoMapper;
using MediatR;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Infrastructure;

namespace MZikmund.Web.Core.Features.Tags;

public class CreateTagHandler : IRequestHandler<CreateTagCommand, Tag>
{
	private readonly IRepository<TagEntity> _tagsRepository;
	private readonly IMapper _mapper;

	public CreateTagHandler(IRepository<TagEntity> tagsRepository, IMapper mapper)
	{
		_tagsRepository = tagsRepository;
		_mapper = mapper;
	}

	public async Task<Tag> Handle(CreateTagCommand request, CancellationToken cancellationToken)
	{
		var tagEntity = _mapper.Map<TagEntity>(request.NewTag);
		var newEntity = await _tagsRepository.AddAsync(tagEntity, cancellationToken);
		return _mapper.Map<Tag>(newEntity);
	}
}
