using MediatR;
using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Web.Core.Infrastructure;

namespace MZikmund.Web.Core.Features.Comments;

public record SetCommentVoteCommand(Guid CommentId, bool Upvoted) : IRequest<CommentDto>, IAuthoredRequest;
