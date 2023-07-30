using MediatR;
using MZikmund.Web.Core.Dtos.Blog;

namespace MZikmund.Web.Core.Blog;

public record GetPostsQuery() : IRequest<Post[]>;
