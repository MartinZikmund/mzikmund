using MediatR;
using MZikmund.DataContracts.Blog;

namespace MZikmund.Web.Core.Blog;

/// <summary>
/// Represents a request to update a category.
/// </summary>
public record UpdateCategoryCommand(Guid CategoryId, EditCategory UpdatedCategory) : IRequest<Category>;
