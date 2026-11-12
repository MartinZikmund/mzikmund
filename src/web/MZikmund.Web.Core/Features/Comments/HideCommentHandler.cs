using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MZikmund.Web.Core.Services;
using MZikmund.Web.Core.Services.Comments;
using MZikmund.Web.Data;

namespace MZikmund.Web.Core.Features.Comments;

internal sealed class HideCommentHandler : IRequestHandler<HideCommentCommand>
{
	private readonly DatabaseContext _dbContext;
	private readonly IDateProvider _dateProvider;
	private readonly ILogger<HideCommentHandler> _logger;

	public HideCommentHandler(
		DatabaseContext dbContext,
		IDateProvider dateProvider,
		ILogger<HideCommentHandler> logger)
	{
		_dbContext = dbContext;
		_dateProvider = dateProvider;
		_logger = logger;
	}

	public async Task Handle(HideCommentCommand request, CancellationToken cancellationToken)
	{
		var resolvedOn = _dateProvider.UtcNow.UtcDateTime;
		var resolvedReports = 0;

		// Every step is conditional on the current state, so EF re-running this after a transient
		// failure cannot count anything twice.
		var strategy = _dbContext.Database.CreateExecutionStrategy();
		await strategy.ExecuteAsync(
			async ct =>
			{
				_dbContext.ChangeTracker.Clear();

				var parentId = await CommentModeration.FindParentIdAsync(_dbContext, request.CommentId, ct);

				// One transaction: the queue entry cannot outlive the hide.
				await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
				resolvedReports = await CommentModeration.ResolveOpenReportsAsync(_dbContext, request.CommentId, resolvedOn, ct);
				await CommentModeration.HideAsync(_dbContext, request.CommentId, parentId, purgeContent: false, ct);
				await transaction.CommitAsync(ct);
			},
			cancellationToken);

		_logger.LogInformation(
			"Comment {CommentId} hidden by moderator; {ResolvedReports} open report(s) auto-resolved.",
			request.CommentId,
			resolvedReports);
	}
}
