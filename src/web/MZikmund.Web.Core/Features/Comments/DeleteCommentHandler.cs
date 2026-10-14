using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MZikmund.Web.Core.Services.Comments;
using MZikmund.Web.Data;
using DataCommentStatus = MZikmund.Web.Data.Entities.CommentStatus;

namespace MZikmund.Web.Core.Features.Comments;

internal sealed class DeleteCommentHandler : IRequestHandler<DeleteCommentCommand>
{
	private readonly DatabaseContext _dbContext;
	private readonly ICurrentUserAccessor _currentUserAccessor;
	private readonly ILogger<DeleteCommentHandler> _logger;

	public DeleteCommentHandler(
		DatabaseContext dbContext,
		ICurrentUserAccessor currentUserAccessor,
		ILogger<DeleteCommentHandler> logger)
	{
		_dbContext = dbContext;
		_currentUserAccessor = currentUserAccessor;
		_logger = logger;
	}

	public async Task Handle(DeleteCommentCommand request, CancellationToken cancellationToken)
	{
		var author = _currentUserAccessor.Current
			?? throw new CommentForbiddenException("A signed-in author is required to delete a comment.");

		var deleted = false;

		// EF re-runs this lambda on a transient failure: reload inside it, and only the writer that
		// flips Visible to DeletedByAuthor decrements the parent's ReplyCount, so a replay after a
		// successful commit is a no-op.
		var strategy = _dbContext.Database.CreateExecutionStrategy();
		await strategy.ExecuteAsync(
			async ct =>
			{
				_dbContext.ChangeTracker.Clear();

				var comment = await _dbContext.Comment
					.AsNoTracking()
					.Where(c => c.Id == request.CommentId)
					.Select(c => new { c.Id, c.AuthorId, c.ParentId, c.Status })
					.FirstOrDefaultAsync(ct)
					?? throw new CommentNotFoundException($"Comment {request.CommentId} was not found.");

				if (comment.AuthorId != author.Id)
				{
					throw new CommentForbiddenException($"Comment {comment.Id} belongs to another author.");
				}

				if (comment.Status == DataCommentStatus.DeletedByAuthor)
				{
					return;
				}

				if (comment.Status == DataCommentStatus.HiddenByModerator)
				{
					throw new CommentConflictException($"Comment {comment.Id} was hidden by a moderator and cannot be deleted.");
				}

				await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

				// The row and its replies stay: a tombstone keeps the thread intact, but the text
				// itself is gone because the author asked for that.
				var updated = await _dbContext.Comment
					.Where(c => c.Id == comment.Id && c.Status == DataCommentStatus.Visible)
					.ExecuteUpdateAsync(
						s => s
							.SetProperty(c => c.Status, DataCommentStatus.DeletedByAuthor)
							.SetProperty(c => c.Content, "")
							.SetProperty(c => c.ContentHtml, ""),
						ct);

				if (updated > 0 && comment.ParentId is Guid parentId)
				{
					await CommentCounters.DecrementReplyCountAsync(_dbContext, parentId, ct);
				}

				await transaction.CommitAsync(ct);
				deleted = updated > 0;
			},
			cancellationToken);

		if (deleted)
		{
			_logger.LogInformation("Comment {CommentId} soft-deleted by its author.", request.CommentId);
		}
	}
}
