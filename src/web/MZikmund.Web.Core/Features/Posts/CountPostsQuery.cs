using MediatR;

namespace MZikmund.Web.Core.Features.Posts;

public record CountPostsQuery(Guid? CategoryId = null, Guid? TagId = null, string? SearchTerm = null) : IRequest<int>;
