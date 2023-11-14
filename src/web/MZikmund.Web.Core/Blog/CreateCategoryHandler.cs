using AutoMapper;
using MediatR;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Infrastructure;

namespace MZikmund.Web.Core.Blog;

public class CreateCategoryHandler : IRequestHandler<CreateCategoryCommand, Category>
{
	private readonly IRepository<CategoryEntity> _tagsRepository;
	private readonly IMapper _mapper;

	public CreateCategoryHandler(IRepository<CategoryEntity> tagsRepository, IMapper mapper)
	{
		_tagsRepository = tagsRepository;
		_mapper = mapper;
	}

	public async Task<Category> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
	{
		var tagEntity = _mapper.Map<CategoryEntity>(request.NewCategory);
		var newEntity = await _tagsRepository.AddAsync(tagEntity, cancellationToken);
		return _mapper.Map<Category>(newEntity);
	}
}
