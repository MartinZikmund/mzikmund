using MediatR;
using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Web.Core.Infrastructure;

namespace MZikmund.Web.Core.Features.Comments;

public record CreateCommentCommand(Guid PostId, string Content, Guid? ParentId) : IRequest<CommentDto>, IAuthoredRequest;
