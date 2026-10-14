using MediatR;
using Microsoft.EntityFrameworkCore;
using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Web.Data;
using DataCommentStatus = MZikmund.Web.Data.Entities.CommentStatus;

namespace MZikmund.Web.Core.Features.Comments;

internal sealed class FindCommentPageHandler : IRequestHandler<FindCommentPageQuery, int?>
{
	private readonly DatabaseContext _dbContext;

	public FindCommentPageHandler(DatabaseContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<int?> Handle(FindCommentPageQuery request, CancellationToken cancellationToken)
	{
		var target = await _dbContext.Comment
			.AsNoTracking()
			.Where(comment => comment.Id == request.CommentId)
			.Select(comment => new { comment.ParentId, comment.Status })
			.FirstOrDefaultAsync(cancellationToken);

		// Deleted and hidden replies are never listed, so there is nothing to scroll to.
		if (target is null || (target.ParentId is not null && target.Status != DataCommentStatus.Visible))
		{
			return null;
		}

		var anchorId = target.ParentId ?? request.CommentId;
		var topLevel = CommentProjection.Readable(_dbContext.Comment.AsNoTracking())
			.Where(comment => comment.ParentId == null);

		var anchor = await topLevel
			.Where(comment => comment.Id == anchorId)
			.Select(comment => new { comment.PostId, comment.CreatedOn, comment.UpvoteCount })
			.FirstOrDefaultAsync(cancellationToken);
		if (anchor is null)
		{
			return null;
		}

		// Mirrors the ordering of GetPostCommentsHandler.
		var siblings = topLevel.Where(comment => comment.PostId == anchor.PostId);
		var precedingCount = request.Sort == CommentSortOrder.New
			? await siblings.CountAsync(comment => comment.CreatedOn > anchor.CreatedOn, cancellationToken)
			: await siblings.CountAsync(
				comment => comment.UpvoteCount > anchor.UpvoteCount
					|| (comment.UpvoteCount == anchor.UpvoteCount && comment.CreatedOn < anchor.CreatedOn),
				cancellationToken);

		var pageSize = Math.Max(request.PageSize, 1);
		return (precedingCount / pageSize) + 1;
	}
}
