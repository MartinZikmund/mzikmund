using MediatR;
using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Web.Core.Infrastructure;

namespace MZikmund.Web.Core.Features.Comments;

public record UpdateCommentCommand(Guid CommentId, string Content) : IRequest<CommentDto>, IAuthoredRequest;
