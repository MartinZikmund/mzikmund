using MediatR;
using MZikmund.DataContracts.Blog;

namespace MZikmund.Web.Core.Blog;

public record GetCategoriesQuery : IRequest<IReadOnlyList<Category>>;
