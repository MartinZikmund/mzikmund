using MediatR;
using Microsoft.EntityFrameworkCore;
using MZikmund.DataContracts;
using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Web.Core.Services.Comments;
using MZikmund.Web.Data;

namespace MZikmund.Web.Core.Features.Comments;

internal sealed class GetCommentRepliesHandler : IRequestHandler<GetCommentRepliesQuery, PagedResponse<CommentDto>>
{
	private readonly DatabaseContext _dbContext;
	private readonly ICurrentUserAccessor _currentUserAccessor;

	public GetCommentRepliesHandler(DatabaseContext dbContext, ICurrentUserAccessor currentUserAccessor)
	{
		_dbContext = dbContext;
		_currentUserAccessor = currentUserAccessor;
	}

	public async Task<PagedResponse<CommentDto>> Handle(GetCommentRepliesQuery request, CancellationToken cancellationToken)
	{
		if (request.PageNumber < 1)
		{
			throw new CommentValidationException("Page number must be at least 1.");
		}

		if (request.PageSize < 1)
		{
			throw new CommentValidationException("Page size must be at least 1.");
		}

		var parentPostId = await CommentProjection.Visible(_dbContext.Comment)
			.Where(comment => comment.Id == request.CommentId)
			.Select(comment => (Guid?)comment.PostId)
			.FirstOrDefaultAsync(cancellationToken);
		if (parentPostId is null
			|| !await CommentPostVisibility.IsPublicAsync(_dbContext.Post, parentPostId.Value, cancellationToken))
		{
			throw new CommentNotFoundException($"Comment {request.CommentId} was not found.");
		}

		var currentAuthorId = _currentUserAccessor.Current?.Id;

		var replies = CommentProjection.Visible(_dbContext.Comment)
			.Where(comment => comment.ParentId == request.CommentId);

		var totalCount = await replies.CountAsync(cancellationToken);

		var rows = await CommentProjection.SelectRows(
			replies
				.OrderBy(comment => comment.CreatedOn)
				.Skip((request.PageNumber - 1) * request.PageSize)
				.Take(request.PageSize),
			currentAuthorId)
			.ToArrayAsync(cancellationToken);

		var comments = rows.Select(row => CommentProjection.ToDto(row)).ToArray();

		return new PagedResponse<CommentDto>(comments, request.PageNumber, request.PageSize, totalCount);
	}
}
