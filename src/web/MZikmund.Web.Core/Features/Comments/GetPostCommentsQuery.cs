using MediatR;
using MZikmund.DataContracts;
using MZikmund.DataContracts.Blog.Comments;

namespace MZikmund.Web.Core.Features.Comments;

public record GetPostCommentsQuery(Guid PostId, CommentSortOrder Sort, int PageNumber, int PageSize) : IRequest<PagedResponse<CommentDto>>;
