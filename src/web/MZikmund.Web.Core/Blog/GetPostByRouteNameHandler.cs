using MediatR;
using MZikmund.Web.Core.Dtos.Blog;

namespace MZikmund.Web.Core.Blog;

public class GetPostByRouteNameHandler : IRequestHandler<GetPostByRouteNameQuery, Post>
{
	public Task<Post> Handle(GetPostByRouteNameQuery request, CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}
}
