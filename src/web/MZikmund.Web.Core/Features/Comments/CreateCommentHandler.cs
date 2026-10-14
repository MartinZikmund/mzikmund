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

internal sealed class CreateCommentHandler : IRequestHandler<CreateCommentCommand, CommentDto>
{
	private readonly DatabaseContext _dbContext;
	private readonly ICurrentUserAccessor _currentUserAccessor;
	private readonly ICommentContentRenderer _contentRenderer;
	private readonly IDateProvider _dateProvider;
	private readonly ILogger<CreateCommentHandler> _logger;

	public CreateCommentHandler(
		DatabaseContext dbContext,
		ICurrentUserAccessor currentUserAccessor,
		ICommentContentRenderer contentRenderer,
		IDateProvider dateProvider,
		ILogger<CreateCommentHandler> logger)
	{
		_dbContext = dbContext;
		_currentUserAccessor = currentUserAccessor;
		_contentRenderer = contentRenderer;
		_dateProvider = dateProvider;
		_logger = logger;
	}

	public async Task<CommentDto> Handle(CreateCommentCommand request, CancellationToken cancellationToken)
	{
		var author = _currentUserAccessor.Current
			?? throw new UnauthorizedAccessException("A signed-in author is required to create a comment.");

		var content = request.Content.Trim();
		if (content.Length == 0)
		{
			throw new ArgumentException("Comment content must not be empty.", nameof(request));
		}

		if (content.Length > CommentLimits.MaxCommentLength)
		{
			throw new ArgumentOutOfRangeException(
				nameof(request),
				$"Comment content must not exceed {CommentLimits.MaxCommentLength} characters.");
		}

		var contentHtml = _contentRenderer.RenderHtml(content);
		var commentId = Guid.NewGuid();
		var now = _dateProvider.UtcNow.UtcDateTime;
		CommentEntity? created = null;

		// EF re-runs this lambda on a transient failure, possibly after the commit already went
		// through: reload everything inside and treat an existing commentId as "done".
		var strategy = _dbContext.Database.CreateExecutionStrategy();
		await strategy.ExecuteAsync(
			async ct =>
			{
				_dbContext.ChangeTracker.Clear();

				var committed = await _dbContext.Comment
					.Include(c => c.Author)
					.Include(c => c.Votes)
					.FirstOrDefaultAsync(c => c.Id == commentId, ct);
				if (committed is not null)
				{
					created = committed;
					return;
				}

				if (!await CommentPostVisibility.IsPublicAsync(_dbContext.Post, request.PostId, ct))
				{
					throw new KeyNotFoundException($"Post {request.PostId} was not found.");
				}

				var authorEntity = await _dbContext.CommentAuthor
					.FirstOrDefaultAsync(a => a.Id == author.Id, ct)
					?? throw new KeyNotFoundException($"Comment author {author.Id} was not found.");

				CommentEntity? parent = null;
				string? mentionedDisplayName = null;

				if (request.ParentId is Guid targetId)
				{
					var target = await _dbContext.Comment
						.Include(c => c.Author)
						.FirstOrDefaultAsync(c => c.Id == targetId, ct)
						?? throw new KeyNotFoundException($"Comment {targetId} was not found.");

					if (target.PostId != request.PostId)
					{
						throw new InvalidOperationException($"Comment {targetId} does not belong to post {request.PostId}.");
					}

					if (target.Status != DataCommentStatus.Visible)
					{
						throw new InvalidOperationException($"Comment {targetId} is not visible and cannot be replied to.");
					}

					if (target.ParentId is Guid grandParentId)
					{
						// One level of threading only. A reply aimed at a reply is re-pointed at the
						// grandparent and attributed to the comment it actually targeted. The mention
						// is a column, never a prefix inside Content.
						parent = await _dbContext.Comment
							.FirstOrDefaultAsync(c => c.Id == grandParentId, ct)
							?? throw new KeyNotFoundException($"Comment {grandParentId} was not found.");

						if (parent.Status != DataCommentStatus.Visible)
						{
							throw new InvalidOperationException($"Comment {grandParentId} is not visible and cannot be replied to.");
						}

						mentionedDisplayName = target.Author.DisplayName;
					}
					else
					{
						parent = target;
					}
				}

				await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);

				CommentEntity comment = new()
				{
					Id = commentId,
					PostId = request.PostId,
					ParentId = parent?.Id,
					AuthorId = authorEntity.Id,
					Author = authorEntity,
					Content = content,
					ContentHtml = contentHtml,
					MentionedDisplayName = mentionedDisplayName,
					Status = DataCommentStatus.Visible,
					CreatedOn = now,
				};

				_dbContext.Comment.Add(comment);
				if (parent is not null)
				{
					parent.ReplyCount += 1;
				}

				await _dbContext.SaveChangesAsync(ct);
				await transaction.CommitAsync(ct);
				created = comment;
			},
			cancellationToken);

		_logger.LogInformation(
			"Comment {CommentId} created on post {PostId} by author {AuthorId}.",
			created!.Id,
			created.PostId,
			author.Id);

		return CommentProjection.ToDto(created, author.Id, includeReplies: false);
	}
}
