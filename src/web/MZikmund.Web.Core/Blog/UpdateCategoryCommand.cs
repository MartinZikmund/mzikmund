using MediatR;
using MZikmund.DataContracts.Blog;

namespace MZikmund.Logic.Requests.Blog.Categories;

/// <summary>
/// Represents a request to update a category.
/// </summary>
public record UpdateCategoryCommand(Guid CategoryId, EditCategory UpdatedCategory) : IRequest<Category>;
