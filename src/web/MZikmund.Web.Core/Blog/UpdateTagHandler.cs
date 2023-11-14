using AutoMapper;
using MediatR;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Infrastructure;

namespace MZikmund.Web.Core.Blog;

internal class UpdateTagHandler : IRequestHandler<UpdateTagCommand, Tag>
{
	private readonly IRepository<TagEntity> _categoriesRepository;
	private readonly IMapper _mapper;

	public UpdateTagHandler(IRepository<TagEntity> categoriesRepository, IMapper mapper)
	{
		_categoriesRepository = categoriesRepository;
		_mapper = mapper;
	}

	public async Task<Tag> Handle(UpdateTagCommand request, CancellationToken cancellationToken)
	{
		var updatedTag = request.UpdatedTag;
		var category = await _categoriesRepository.GetAsync(request.TagId, cancellationToken);
		if (category is null)
		{
			throw new KeyNotFoundException("Tag not found"); // TODO: Use a more descriptive exception.
		}

		category.DisplayName = updatedTag.DisplayName;
		category.RouteName = updatedTag.RouteName;
		category.Description = updatedTag.Description;

		await _categoriesRepository.UpdateAsync(category, cancellationToken);

		return _mapper.Map<Tag>(category);
	}
}
