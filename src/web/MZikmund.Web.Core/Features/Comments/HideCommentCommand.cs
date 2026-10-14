using MediatR;

namespace MZikmund.Web.Core.Features.Comments;

public record HideCommentCommand(Guid CommentId) : IRequest;
