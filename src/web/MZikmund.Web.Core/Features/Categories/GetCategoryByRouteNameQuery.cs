using MediatR;
using MZikmund.DataContracts.Blog;

namespace MZikmund.Web.Core.Features.Categories;

public record GetCategoryByRouteNameQuery(string RouteName) : IRequest<Category>;
