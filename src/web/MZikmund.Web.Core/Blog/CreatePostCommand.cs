using MediatR;
using MZikmund.DataContracts.Blog;

namespace MZikmund.Web.Core.Blog;

public record CreatePostCommand(Post NewPost) : IRequest<Post>;
