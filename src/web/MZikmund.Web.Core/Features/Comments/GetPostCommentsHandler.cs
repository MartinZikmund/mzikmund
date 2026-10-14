using MediatR;
using Microsoft.EntityFrameworkCore;
using MZikmund.DataContracts;
using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Web.Core.Services.Comments;
using MZikmund.Web.Data;
using MZikmund.Web.Data.Entities;

namespace MZikmund.Web.Core.Features.Comments;

internal sealed class GetPostCommentsHandler : IRequestHandler<GetPostCommentsQuery, PagedResponse<CommentDto>>
{
	private readonly DatabaseContext _dbContext;
	private readonly ICurrentUserAccessor _currentUserAccessor;

	public GetPostCommentsHandler(DatabaseContext dbContext, ICurrentUserAccessor currentUserAccessor)
	{
		_dbContext = dbContext;
		_currentUserAccessor = currentUserAccessor;
	}

	public async Task<PagedResponse<CommentDto>> Handle(GetPostCommentsQuery request, CancellationToken cancellationToken)
	{
		CommentPaging.Validate(request.PageNumber, request.PageSize);

		if (!await CommentPostVisibility.IsPublicAsync(_dbContext.Post, request.PostId, cancellationToken))
		{
			throw new CommentNotFoundException($"Post {request.PostId} was not found.");
		}

		var currentAuthorId = _currentUserAccessor.Current?.Id;

		var topLevel = CommentProjection.Readable(_dbContext.Comment)
			.Where(comment => comment.PostId == request.PostId && comment.ParentId == null);

		var totalCount = await topLevel.CountAsync(cancellationToken);

		IOrderedQueryable<CommentEntity> ordered = request.Sort == CommentSortOrder.Top
			? topLevel.OrderByDescending(comment => comment.UpvoteCount).ThenBy(comment => comment.CreatedOn).ThenBy(comment => comment.Id)
			: topLevel.OrderByDescending(comment => comment.CreatedOn).ThenByDescending(comment => comment.Id);

		var rows = await CommentProjection.SelectRows(
			ordered
				.Skip((request.PageNumber - 1) * request.PageSize)
				.Take(request.PageSize),
			currentAuthorId)
			.ToArrayAsync(cancellationToken);

		var repliesByParentId = await CommentProjection.LoadInlineRepliesAsync(
			_dbContext.Comment,
			rows.Select(row => row.Id).ToArray(),
			currentAuthorId,
			cancellationToken);

		var comments = rows
			.Select(row => CommentProjection.ToDto(row, repliesByParentId[row.Id]))
			.ToArray();

		return new PagedResponse<CommentDto>(comments, request.PageNumber, request.PageSize, totalCount);
	}
}
