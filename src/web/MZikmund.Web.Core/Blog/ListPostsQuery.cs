using MediatR;
using MZikmund.Web.Core.Dtos;

namespace MZikmund.Web.Core.Blog;

public record ListPostsQuery(int Page, int PageSize) : IRequest<IReadOnlyList<PostListItem>>;
