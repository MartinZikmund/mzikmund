using MediatR;
using MZikmund.DataContracts.Blog;

namespace MZikmund.Web.Core.Blog;

public record GetPostByRouteNameQuery(string RouteName) : IRequest<Post>;
