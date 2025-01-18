using MediatR;
using MZikmund.DataContracts.Blog;

namespace MZikmund.Web.Core.Features.Posts;

public record GetPostByRouteNameQuery(string RouteName) : IRequest<Post>;
