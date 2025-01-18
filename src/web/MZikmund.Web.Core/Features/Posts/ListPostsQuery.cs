using MediatR;
using MZikmund.DataContracts;
using MZikmund.DataContracts.Blog;

namespace MZikmund.Web.Core.Features.Posts;

public record ListPostsQuery(int Page, int PageSize, Guid? CategoryId = null, Guid? TagId = null) : IRequest<PagedResponse<PostListItem>>;
