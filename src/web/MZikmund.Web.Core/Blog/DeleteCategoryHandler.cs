using MediatR;
using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Infrastructure;

namespace MZikmund.Web.Core.Blog;

public class DeleteCategoryHandler : IRequestHandler<DeleteCategoryCommand>
{
	private readonly IRepository<CategoryEntity> _categoriesRepository;

	public DeleteCategoryHandler(IRepository<CategoryEntity> categoriesRepository)
	{
		_categoriesRepository = categoriesRepository ?? throw new ArgumentNullException(nameof(categoriesRepository));
	}

	public async Task Handle(DeleteCategoryCommand request, CancellationToken cancellationToken) =>
		await _categoriesRepository.DeleteAsync(request.CategoryId, cancellationToken);
}
