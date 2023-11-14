using MediatR;
using MZikmund.DataContracts.Blog;

namespace MZikmund.Web.Core.Blog;

public record GetCategoryByRouteNameQuery(string RouteName) : IRequest<Category>;
