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

		// EF re-runs this lambda on a transient failure: reload inside it so the parent's
		// ReplyCount is decremented once, and a replay after a successful commit is a no-op.
		var strategy = _dbContext.Database.CreateExecutionStrategy();
		await strategy.ExecuteAsync(
			async ct =>
			{
				_dbContext.ChangeTracker.Clear();

				var comment = await _dbContext.Comment
					.FirstOrDefaultAsync(c => c.Id == request.CommentId, ct)
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
				comment.Status = DataCommentStatus.DeletedByAuthor;
				comment.Content = "";
				comment.ContentHtml = "";

				if (comment.ParentId is Guid parentId)
				{
					var parent = await _dbContext.Comment.FirstOrDefaultAsync(c => c.Id == parentId, ct);
					if (parent is not null && parent.ReplyCount > 0)
					{
						parent.ReplyCount -= 1;
					}
				}

				await _dbContext.SaveChangesAsync(ct);
				await transaction.CommitAsync(ct);
				deleted = true;
			},
			cancellationToken);

		if (deleted)
		{
			_logger.LogInformation("Comment {CommentId} soft-deleted by its author.", request.CommentId);
		}
	}
}
