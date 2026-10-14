using MediatR;
using MZikmund.DataContracts;
using MZikmund.DataContracts.Blog.Comments;

namespace MZikmund.Web.Core.Features.Comments;

public record GetModerationFeedQuery(DateTime? Since, int PageNumber, int PageSize) : IRequest<PagedResponse<CommentDto>>;
