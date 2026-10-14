using MediatR;
using MZikmund.DataContracts.Blog.Comments;

namespace MZikmund.Web.Core.Features.Comments;

/// <summary>
/// Resolves the 1-based page of top-level comments that contains the given comment, or its
/// parent when it is a reply. Returns <c>null</c> when the comment cannot be shown.
/// </summary>
public record FindCommentPageQuery(Guid CommentId, CommentSortOrder Sort, int PageSize) : IRequest<int?>;
