using MediatR;

namespace MZikmund.Web.Core.Features.Posts;

/// <summary>
/// Query to count all posts without filtering by published status (for admin use).
/// </summary>
public record CountAllPostsQuery(Guid? CategoryId = null, Guid? TagId = null) : IRequest<int>;
