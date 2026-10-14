using MediatR;
using Microsoft.EntityFrameworkCore;
using MZikmund.DataContracts;
using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Web.Data;

namespace MZikmund.Web.Core.Features.Comments;

internal sealed class GetCommentReportsHandler : IRequestHandler<GetCommentReportsQuery, PagedResponse<CommentReportDto>>
{
	private readonly DatabaseContext _dbContext;

	public GetCommentReportsHandler(DatabaseContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<PagedResponse<CommentReportDto>> Handle(GetCommentReportsQuery request, CancellationToken cancellationToken)
	{
		ModerationPaging.Validate(request.PageNumber, request.PageSize);

		var openReports = _dbContext.CommentReport.Where(r => r.ResolvedOn == null);

		// One queue row per reported comment: ten reports on one comment count once.
		var totalCount = await openReports
			.Select(r => r.CommentId)
			.Distinct()
			.CountAsync(cancellationToken);

		var groups = await openReports
			.GroupBy(r => r.CommentId)
			.Select(g => new
			{
				CommentId = g.Key,
				OpenReportCount = g.Count(),
				FirstReportedOn = g.Min(r => r.CreatedOn),
			})
			.OrderBy(g => g.FirstReportedOn)
			.ThenBy(g => g.CommentId)
			.Skip((request.PageNumber - 1) * request.PageSize)
			.Take(request.PageSize)
			.ToArrayAsync(cancellationToken);

		var commentIds = groups.Select(g => g.CommentId).ToArray();

		var reasons = await openReports
			.Where(r => commentIds.Contains(r.CommentId))
			.Select(r => new { r.CommentId, r.Reason })
			.ToArrayAsync(cancellationToken);

		var comments = await _dbContext.Comment
			.Where(c => commentIds.Contains(c.Id))
			.Include(c => c.Author)
			.ToDictionaryAsync(c => c.Id, cancellationToken);

		var items = groups.Select(group =>
		{
			var comment = comments[group.CommentId];
			return new CommentReportDto
			{
				CommentId = group.CommentId,
				AuthorId = comment.AuthorId,
				Comment = ModerationProjection.ToDto(comment),
				OpenReportCount = group.OpenReportCount,
				Reasons = reasons
					.Where(r => r.CommentId == group.CommentId)
					.Select(r => r.Reason)
					.Distinct()
					.OrderBy(r => r)
					.ToArray(),
				FirstReportedOn = group.FirstReportedOn,
			};
		}).ToArray();

		return new PagedResponse<CommentReportDto>(items, request.PageNumber, request.PageSize, totalCount);
	}
}
