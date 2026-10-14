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

internal sealed class SetCommentVoteHandler : IRequestHandler<SetCommentVoteCommand, CommentDto>
{
	private readonly DatabaseContext _dbContext;
	private readonly ICurrentUserAccessor _currentUserAccessor;
	private readonly IDateProvider _dateProvider;
	private readonly ILogger<SetCommentVoteHandler> _logger;

	public SetCommentVoteHandler(
		DatabaseContext dbContext,
		ICurrentUserAccessor currentUserAccessor,
		IDateProvider dateProvider,
		ILogger<SetCommentVoteHandler> logger)
	{
		_dbContext = dbContext;
		_currentUserAccessor = currentUserAccessor;
		_dateProvider = dateProvider;
		_logger = logger;
	}

	public async Task<CommentDto> Handle(SetCommentVoteCommand request, CancellationToken cancellationToken)
	{
		var author = _currentUserAccessor.Current
			?? throw new CommentForbiddenException("A signed-in author is required to vote on a comment.");

		CommentEntity? voted = null;

		// EF re-runs this lambda on a transient failure: the vote state is re-read each time, so a
		// replay after a successful commit changes nothing. The counter moves with an atomic UPDATE
		// in the same transaction as the vote row, so concurrent voters cannot lose an increment.
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

				if (comment.AuthorId == author.Id)
				{
					throw new CommentConflictException("An author cannot vote on their own comment.");
				}

				if (comment.Status != DataCommentStatus.Visible)
				{
					throw new CommentConflictException($"Comment {comment.Id} is not visible and cannot be voted on.");
				}

				var hasVoted = await VoteExistsAsync(comment.Id, author.Id, ct);

				// Voting twice and un-voting twice are deliberate no-ops: PUT and DELETE are idempotent.
				if (request.Upvoted && !hasVoted)
				{
					try
					{
						await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
						_dbContext.CommentVote.Add(new CommentVoteEntity
						{
							CommentId = comment.Id,
							AuthorId = author.Id,
							CreatedOn = _dateProvider.UtcNow.UtcDateTime,
						});
						await _dbContext.SaveChangesAsync(ct);
						await CommentCounters.IncrementUpvoteCountAsync(_dbContext, comment.Id, ct);
						await transaction.CommitAsync(ct);
					}
					catch (DbUpdateException)
					{
						// A concurrent request from the same user inserted the vote first (primary key
						// hit): the vote exists, which is exactly what was asked for.
						_dbContext.ChangeTracker.Clear();
						if (!await VoteExistsAsync(comment.Id, author.Id, ct))
						{
							throw;
						}
					}
				}
				else if (!request.Upvoted && hasVoted)
				{
					await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
					var removed = await _dbContext.CommentVote
						.Where(v => v.CommentId == comment.Id && v.AuthorId == author.Id)
						.ExecuteDeleteAsync(ct);
					if (removed > 0)
					{
						await CommentCounters.DecrementUpvoteCountAsync(_dbContext, comment.Id, ct);
					}

					await transaction.CommitAsync(ct);
				}

				_dbContext.ChangeTracker.Clear();
				voted = await _dbContext.Comment
					.Include(c => c.Author)
					.Include(c => c.Votes)
					.FirstAsync(c => c.Id == request.CommentId, ct);
			},
			cancellationToken);

		_logger.LogInformation(
			"Vote on comment {CommentId} by author {AuthorId} set to {Upvoted}.",
			voted!.Id,
			author.Id,
			request.Upvoted);

		return CommentProjection.ToDto(voted, author.Id, includeReplies: false);
	}

	private Task<bool> VoteExistsAsync(Guid commentId, Guid authorId, CancellationToken cancellationToken) =>
		_dbContext.CommentVote.AnyAsync(v => v.CommentId == commentId && v.AuthorId == authorId, cancellationToken);
}
