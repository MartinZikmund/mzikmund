using MediatR;
using MZikmund.Web.Core.Dtos;

namespace MZikmund.Web.Core.Blog;

public record GetPostByRouteNameQuery(string RouteName) : IRequest<Post>;
