using MediatR;
using MZikmund.DataContracts.Blog;

namespace MZikmund.Web.Core.Features.Posts;

public record CreatePostCommand(Post NewPost) : IRequest<Post>;
