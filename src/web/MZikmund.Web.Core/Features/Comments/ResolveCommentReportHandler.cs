using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MZikmund.Web.Core.Services;
using MZikmund.Web.Core.Services.Comments;
using MZikmund.Web.Data;

namespace MZikmund.Web.Core.Features.Comments;

internal sealed class ResolveCommentReportHandler : IRequestHandler<ResolveCommentReportCommand>
{
	private readonly DatabaseContext _dbContext;
	private readonly IDateProvider _dateProvider;
	private readonly ILogger<ResolveCommentReportHandler> _logger;

	public ResolveCommentReportHandler(
		DatabaseContext dbContext,
		IDateProvider dateProvider,
		ILogger<ResolveCommentReportHandler> logger)
	{
		_dbContext = dbContext;
		_dateProvider = dateProvider;
		_logger = logger;
	}

	public async Task Handle(ResolveCommentReportCommand request, CancellationToken cancellationToken)
	{
		var comment = await _dbContext.Comment
			.FirstOrDefaultAsync(c => c.Id == request.CommentId, cancellationToken)
			?? throw new CommentNotFoundException($"Comment {request.CommentId} was not found.");

		// "Reviewed and dismissed": the comment itself is left exactly as it is.
		var resolvedReports = await CommentModeration.ResolveOpenReportsAsync(
			_dbContext, comment, _dateProvider.UtcNow.UtcDateTime, cancellationToken);

		await _dbContext.SaveChangesAsync(cancellationToken);

		_logger.LogInformation(
			"{ResolvedReports} open report(s) on comment {CommentId} dismissed by moderator.",
			resolvedReports,
			comment.Id);
	}
}
