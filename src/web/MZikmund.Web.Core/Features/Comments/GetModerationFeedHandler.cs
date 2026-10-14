using MediatR;
using Microsoft.EntityFrameworkCore;
using MZikmund.DataContracts;
using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Web.Data;

namespace MZikmund.Web.Core.Features.Comments;

internal sealed class GetModerationFeedHandler : IRequestHandler<GetModerationFeedQuery, PagedResponse<CommentDto>>
{
	private readonly DatabaseContext _dbContext;

	public GetModerationFeedHandler(DatabaseContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<PagedResponse<CommentDto>> Handle(GetModerationFeedQuery request, CancellationToken cancellationToken)
	{
		CommentPaging.Validate(request.PageNumber, request.PageSize);

		var query = CommentModerationFeed.Since(_dbContext, request.Since);
		var totalCount = await query.CountAsync(cancellationToken);

		var comments = await query
			.Include(c => c.Author)
			.OrderByDescending(c => c.CreatedOn)
			.ThenByDescending(c => c.Id)
			.Skip((request.PageNumber - 1) * request.PageSize)
			.Take(request.PageSize)
			.ToArrayAsync(cancellationToken);

		var items = comments.Select(ModerationProjection.ToDto).ToArray();
		return new PagedResponse<CommentDto>(items, request.PageNumber, request.PageSize, totalCount);
	}
}
