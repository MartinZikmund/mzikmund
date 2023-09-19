using MediatR;
using MZikmund.Web.Core.Dtos;

namespace MZikmund.Web.Core.Blog;

public record GetCategoriesQuery : IRequest<IReadOnlyList<Category>>;
