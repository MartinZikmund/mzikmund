using MediatR;
using MZikmund.Web.Core.Dtos;

namespace MZikmund.Web.Core.Blog;

public record CreateCategoryCommand : IRequest<IReadOnlyList<Category>>;
