using MediatR;
using MZikmund.DataContracts.Blog;

namespace MZikmund.Web.Core.Features.Categories;

public record GetCategoriesWithPostCountQuery : IRequest<IReadOnlyList<CategoryWithPostCount>>;
