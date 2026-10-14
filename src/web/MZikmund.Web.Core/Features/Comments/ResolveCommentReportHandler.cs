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
		var resolvedOn = _dateProvider.UtcNow.UtcDateTime;
		var resolvedReports = 0;

		var strategy = _dbContext.Database.CreateExecutionStrategy();
		await strategy.ExecuteAsync(
			async ct =>
			{
				_dbContext.ChangeTracker.Clear();

				await CommentModeration.FindParentIdAsync(_dbContext, request.CommentId, ct);

				// "Reviewed and dismissed": the comment itself is left exactly as it is.
				await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
				resolvedReports = await CommentModeration.ResolveOpenReportsAsync(_dbContext, request.CommentId, resolvedOn, ct);
				await transaction.CommitAsync(ct);
			},
			cancellationToken);

		_logger.LogInformation(
			"{ResolvedReports} open report(s) on comment {CommentId} dismissed by moderator.",
			resolvedReports,
			request.CommentId);
	}
}
