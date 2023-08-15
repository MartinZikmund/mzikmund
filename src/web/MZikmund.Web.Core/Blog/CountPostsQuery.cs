using MediatR;

namespace MZikmund.Web.Core.Blog;

public record CountPostsQuery(Guid? CategoryId = null, Guid? TagId = null) : IRequest<int>;
