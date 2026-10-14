using MediatR;
using MZikmund.DataContracts;
using MZikmund.DataContracts.Blog.Comments;

namespace MZikmund.Web.Core.Features.Comments;

public record GetCommentRepliesQuery(Guid CommentId, int PageNumber, int PageSize) : IRequest<PagedResponse<CommentDto>>;
