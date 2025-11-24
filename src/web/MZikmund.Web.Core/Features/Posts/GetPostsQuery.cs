using MediatR;
using MZikmund.DataContracts;
using MZikmund.DataContracts.Blog;

namespace MZikmund.Web.Core.Features.Posts;

public record GetPostsQuery(int Page, int PageSize, Guid? CategoryId = null, Guid? TagId = null, string? SearchTerm = null) : IRequest<PagedResponse<PostListItem>>;
