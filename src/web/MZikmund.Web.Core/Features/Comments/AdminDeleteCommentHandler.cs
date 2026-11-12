using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MZikmund.Web.Core.Services;
using MZikmund.Web.Core.Services.Comments;
using MZikmund.Web.Data;

namespace MZikmund.Web.Core.Features.Comments;

internal sealed class AdminDeleteCommentHandler : IRequestHandler<AdminDeleteCommentCommand>
{
	private readonly DatabaseContext _dbContext;
	private readonly IDateProvider _dateProvider;
	private readonly ILogger<AdminDeleteCommentHandler> _logger;

	public AdminDeleteCommentHandler(
		DatabaseContext dbContext,
		IDateProvider dateProvider,
		ILogger<AdminDeleteCommentHandler> logger)
	{
		_dbContext = dbContext;
		_dateProvider = dateProvider;
		_logger = logger;
	}

	public async Task Handle(AdminDeleteCommentCommand request, CancellationToken cancellationToken)
	{
		var resolvedOn = _dateProvider.UtcNow.UtcDateTime;
		var resolvedReports = 0;

		var strategy = _dbContext.Database.CreateExecutionStrategy();
		await strategy.ExecuteAsync(
			async ct =>
			{
				_dbContext.ChangeTracker.Clear();

				var parentId = await CommentModeration.FindParentIdAsync(_dbContext, request.CommentId, ct);

				// The row stays so replies keep their parent; only the text is purged.
				await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
				resolvedReports = await CommentModeration.ResolveOpenReportsAsync(_dbContext, request.CommentId, resolvedOn, ct);
				await CommentModeration.HideAsync(_dbContext, request.CommentId, parentId, purgeContent: true, ct);
				await transaction.CommitAsync(ct);
			},
			cancellationToken);

		_logger.LogInformation(
			"Comment {CommentId} deleted by moderator; {ResolvedReports} open report(s) auto-resolved.",
			request.CommentId,
			resolvedReports);
	}
}
