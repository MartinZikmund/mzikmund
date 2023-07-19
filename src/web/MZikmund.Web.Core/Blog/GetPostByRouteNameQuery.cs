using MediatR;
using MZikmund.Web.Core.Dtos.Blog;

namespace MZikmund.Web.Core.Blog;

public record GetPostByRouteNameQuery : IRequest<Post>;
