using MediatR;
using MZikmund.DataContracts;
using MZikmund.DataContracts.Blog;

namespace MZikmund.Web.Core.Features.Posts;

/// <summary>
/// Query to get all posts without filtering by published status (for admin use).
/// </summary>
public record GetAllPostsQuery(int Page, int PageSize, Guid? CategoryId = null, Guid? TagId = null) : IRequest<PagedResponse<PostListItem>>;
