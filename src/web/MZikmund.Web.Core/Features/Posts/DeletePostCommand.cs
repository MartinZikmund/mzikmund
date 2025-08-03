using MediatR;

namespace MZikmund.Web.Core.Features.Posts;

public record DeletePostCommand(Guid postId, bool softDelete) : IRequest;
