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
			?? throw new UnauthorizedAccessException("A signed-in author is required to vote on a comment.");

		CommentEntity? voted = null;

		// EF re-runs this lambda on a transient failure: the vote state is re-read each time, so a
		// replay after a successful commit changes nothing and the count cannot drift.
		var strategy = _dbContext.Database.CreateExecutionStrategy();
		await strategy.ExecuteAsync(
			async ct =>
			{
				_dbContext.ChangeTracker.Clear();

				var comment = await _dbContext.Comment
					.Include(c => c.Author)
					.Include(c => c.Votes)
					.FirstOrDefaultAsync(c => c.Id == request.CommentId, ct)
					?? throw new KeyNotFoundException($"Comment {request.CommentId} was not found.");

				if (comment.AuthorId == author.Id)
				{
					throw new InvalidOperationException("An author cannot vote on their own comment.");
				}

				if (comment.Status != DataCommentStatus.Visible)
				{
					throw new InvalidOperationException($"Comment {comment.Id} is not visible and cannot be voted on.");
				}

				var existingVote = comment.Votes.FirstOrDefault(v => v.AuthorId == author.Id);

				// Voting twice and un-voting twice are deliberate no-ops: PUT and DELETE are idempotent.
				if (request.Upvoted && existingVote is null)
				{
					await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
					_dbContext.CommentVote.Add(new CommentVoteEntity
					{
						CommentId = comment.Id,
						AuthorId = author.Id,
						CreatedOn = _dateProvider.UtcNow.UtcDateTime,
					});
					comment.UpvoteCount += 1;
					await _dbContext.SaveChangesAsync(ct);
					await transaction.CommitAsync(ct);
				}
				else if (!request.Upvoted && existingVote is not null)
				{
					await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
					_dbContext.CommentVote.Remove(existingVote);
					comment.UpvoteCount = Math.Max(0, comment.UpvoteCount - 1);
					await _dbContext.SaveChangesAsync(ct);
					await transaction.CommitAsync(ct);
				}

				voted = comment;
			},
			cancellationToken);

		_logger.LogInformation(
			"Vote on comment {CommentId} by author {AuthorId} set to {Upvoted}.",
			voted!.Id,
			author.Id,
			request.Upvoted);

		return CommentProjection.ToDto(voted, author.Id, includeReplies: false);
	}
}
