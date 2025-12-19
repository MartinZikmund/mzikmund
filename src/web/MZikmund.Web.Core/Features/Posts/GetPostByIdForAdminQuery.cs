using MediatR;
using MZikmund.DataContracts.Blog;

namespace MZikmund.Web.Core.Features.Posts;

/// <summary>
/// Query to get a post by ID without filtering by published status (for admin use).
/// </summary>
public record GetPostByIdForAdminQuery(Guid Id) : IRequest<Post>;
