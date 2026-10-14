using MediatR;
using Microsoft.EntityFrameworkCore;
using MZikmund.DataContracts;
using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Web.Core.Services;
using MZikmund.Web.Core.Services.Comments;
using MZikmund.Web.Data;

namespace MZikmund.Web.Core.Features.Comments;

internal sealed class GetCommentRepliesHandler : IRequestHandler<GetCommentRepliesQuery, PagedResponse<CommentDto>>
{
	private readonly DatabaseContext _dbContext;
	private readonly ICurrentUserAccessor _currentUserAccessor;
	private readonly IDateProvider _dateProvider;

	public GetCommentRepliesHandler(
		DatabaseContext dbContext,
		ICurrentUserAccessor currentUserAccessor,
		IDateProvider dateProvider)
	{
		_dbContext = dbContext;
		_currentUserAccessor = currentUserAccessor;
		_dateProvider = dateProvider;
	}

	public async Task<PagedResponse<CommentDto>> Handle(GetCommentRepliesQuery request, CancellationToken cancellationToken)
	{
		CommentPaging.Validate(request.PageNumber, request.PageSize);

		var parentPostId = await CommentProjection.Readable(_dbContext.Comment)
			.Where(comment => comment.Id == request.CommentId)
			.Select(comment => (Guid?)comment.PostId)
			.FirstOrDefaultAsync(cancellationToken);
		if (parentPostId is null
			|| !await CommentPostVisibility.IsPublicAsync(_dbContext.Post, parentPostId.Value, _dateProvider.UtcNow, cancellationToken))
		{
			throw new CommentNotFoundException($"Comment {request.CommentId} was not found.");
		}

		var currentAuthorId = _currentUserAccessor.Current?.Id;

		var replies = CommentProjection.VisibleRepliesOf(_dbContext.Comment, request.CommentId);

		var totalCount = await replies.CountAsync(cancellationToken);

		var rows = await CommentProjection.SelectRows(
			replies
				.OrderBy(comment => comment.CreatedOn)
				.ThenBy(comment => comment.Id)
				.Skip((request.PageNumber - 1) * request.PageSize)
				.Take(request.PageSize),
			currentAuthorId)
			.ToArrayAsync(cancellationToken);

		var comments = rows.Select(row => CommentProjection.ToDto(row)).ToArray();

		return new PagedResponse<CommentDto>(comments, request.PageNumber, request.PageSize, totalCount);
	}
}
