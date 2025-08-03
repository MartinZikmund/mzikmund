using MediatR;
using MZikmund.DataContracts.Blog;

namespace MZikmund.Web.Core.Features.Categories;

/// <summary>
/// Represents a request to update a category.
/// </summary>
public record UpdateCategoryCommand(Guid CategoryId, Category UpdatedCategory) : IRequest<Category>;
