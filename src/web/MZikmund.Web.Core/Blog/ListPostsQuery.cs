using MediatR;
using MZikmund.Web.Core.Dtos;

namespace MZikmund.Web.Core.Blog;

public record ListPostsQuery(int Page, int PageSize, Guid? CategoryId = null, Guid? TagId = null) : IRequest<IReadOnlyList<PostListItem>>;
