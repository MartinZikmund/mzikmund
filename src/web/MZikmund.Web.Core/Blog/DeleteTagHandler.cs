using MediatR;
using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Infrastructure;

namespace MZikmund.Web.Core.Blog;

public class DeleteTagHandler : IRequestHandler<DeleteTagCommand>
{
	private readonly IRepository<TagEntity> _categoriesRepository;

	public DeleteTagHandler(IRepository<TagEntity> categoriesRepository)
	{
		_categoriesRepository = categoriesRepository ?? throw new ArgumentNullException(nameof(categoriesRepository));
	}

	public async Task Handle(DeleteTagCommand request, CancellationToken cancellationToken) =>
		await _categoriesRepository.DeleteAsync(request.TagId, cancellationToken);
}
