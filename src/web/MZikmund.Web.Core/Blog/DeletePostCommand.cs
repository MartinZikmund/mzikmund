using MediatR;

namespace MZikmund.Web.Core.Blog;

public record DeletePostCommand(Guid postId, bool softDelete) : IRequest;
