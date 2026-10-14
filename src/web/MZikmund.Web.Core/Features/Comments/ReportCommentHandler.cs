using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Web.Core.Services;
using MZikmund.Web.Core.Services.Comments;
using MZikmund.Web.Data;
using MZikmund.Web.Data.Entities;
using DataCommentStatus = MZikmund.Web.Data.Entities.CommentStatus;

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
			?? throw new CommentForbiddenException("A signed-in author is required to report a comment.");

		if (!Enum.IsDefined(request.Reason))
		{
			throw new CommentValidationException($"Unknown report reason {(int)request.Reason}.");
		}

		var note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
		if (note is not null && note.Length > CommentLimits.MaxReportNoteLength)
		{
			throw new CommentValidationException(
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
					.AsNoTracking()
					.Where(c => c.Id == request.CommentId)
					.Select(c => new { c.Id, c.AuthorId, c.Status })
					.FirstOrDefaultAsync(ct)
					?? throw new CommentNotFoundException($"Comment {request.CommentId} was not found.");

				if (comment.Status != DataCommentStatus.Visible)
				{
					throw new CommentConflictException($"Comment {comment.Id} is not visible and cannot be reported.");
				}

				if (comment.AuthorId == reporter.Id)
				{
					throw new CommentConflictException("An author cannot report their own comment.");
				}

				if (await OpenReportExistsAsync(comment.Id, reporter.Id, ct))
				{
					return;
				}

				try
				{
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
					await _dbContext.SaveChangesAsync(ct);
					await CommentCounters.IncrementReportCountAsync(_dbContext, comment.Id, ct);
					await transaction.CommitAsync(ct);
					reported = true;
				}
				catch (DbUpdateException)
				{
					// The unique index on open reports fired: a concurrent request from the same
					// reporter got there first, so this one is the duplicate and changes nothing.
					_dbContext.ChangeTracker.Clear();
					if (!await OpenReportExistsAsync(comment.Id, reporter.Id, ct))
					{
						throw;
					}
				}
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

	private Task<bool> OpenReportExistsAsync(Guid commentId, Guid reporterId, CancellationToken cancellationToken) =>
		_dbContext.CommentReport.AnyAsync(
			r => r.CommentId == commentId && r.ReporterId == reporterId && r.ResolvedOn == null,
			cancellationToken);
}
