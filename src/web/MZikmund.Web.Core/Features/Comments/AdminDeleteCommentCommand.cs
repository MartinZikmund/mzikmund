using MediatR;

namespace MZikmund.Web.Core.Features.Comments;

public record AdminDeleteCommentCommand(Guid CommentId) : IRequest;
