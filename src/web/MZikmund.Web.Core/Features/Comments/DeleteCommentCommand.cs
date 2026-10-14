using MediatR;
using MZikmund.Web.Core.Infrastructure;

namespace MZikmund.Web.Core.Features.Comments;

public record DeleteCommentCommand(Guid CommentId) : IRequest, IAuthoredRequest, IAllowsBannedAuthor;
