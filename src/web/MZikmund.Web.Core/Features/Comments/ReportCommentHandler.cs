using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Web.Core.Services;
using MZikmund.Web.Core.Services.Comments;
using MZikmund.Web.Data;
using MZikmund.Web.Data.Entities;

namespace MZikmund.Web.Core.Features.Comments;

internal sealed class ReportCommentHandler : IRequestHandler<ReportCommentCommand>
{
	private readonly DatabaseContext _dbContext;
	private readonly ICurrentUserAccessor _currentUserAccessor;
	private readonly IDateProvider _dateProvider;
	private readonly ILogger<ReportCommentHandler> _logger;

	public ReportCommentHandler(
		DatabaseContext dbContext,
		ICurrentUserAccessor currentUserAccessor,
		IDateProvider dateProvider,
		ILogger<ReportCommentHandler> logger)
	{
		_dbContext = dbContext;
		_currentUserAccessor = currentUserAccessor;
		_dateProvider = dateProvider;
		_logger = logger;
	}

	public async Task Handle(ReportCommentCommand request, CancellationToken cancellationToken)
	{
		var reporter = _currentUserAccessor.Current
			?? throw new UnauthorizedAccessException("A signed-in author is required to report a comment.");

		var note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
		if (note is not null && note.Length > CommentLimits.MaxReportNoteLength)
		{
			throw new ArgumentOutOfRangeException(
				nameof(request),
				$"Report note must not exceed {CommentLimits.MaxReportNoteLength} characters.");
		}

		var createdOn = _dateProvider.UtcNow.UtcDateTime;
		var reported = false;

		// EF re-runs this lambda on a transient failure: the open-report check is repeated inside it,
		// so a replay after a successful commit cannot count the same report twice.
		var strategy = _dbContext.Database.CreateExecutionStrategy();
		await strategy.ExecuteAsync(
			async ct =>
			{
				_dbContext.ChangeTracker.Clear();

				var comment = await _dbContext.Comment
					.FirstOrDefaultAsync(c => c.Id == request.CommentId, ct)
					?? throw new KeyNotFoundException($"Comment {request.CommentId} was not found.");

				var alreadyReported = await _dbContext.CommentReport
					.AnyAsync(r => r.CommentId == comment.Id && r.ReporterId == reporter.Id && r.ResolvedOn == null, ct);
				if (alreadyReported)
				{
					return;
				}

				await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
				_dbContext.CommentReport.Add(new CommentReportEntity
				{
					Id = Guid.NewGuid(),
					CommentId = comment.Id,
					ReporterId = reporter.Id,
					Reason = request.Reason,
					Note = note,
					CreatedOn = createdOn,
				});
				comment.ReportCount += 1;
				await _dbContext.SaveChangesAsync(ct);
				await transaction.CommitAsync(ct);
				reported = true;
			},
			cancellationToken);

		if (reported)
		{
			_logger.LogInformation(
				"Comment {CommentId} reported by author {ReporterId} for {Reason}.",
				request.CommentId,
				reporter.Id,
				request.Reason);
		}
	}
}
