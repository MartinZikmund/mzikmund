using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Web.Core.Services;
using MZikmund.Web.Core.Services.Comments;
using MZikmund.Web.Data;
using DataCommentStatus = MZikmund.Web.Data.Entities.CommentStatus;

namespace MZikmund.Web.Core.Features.Comments;

internal sealed class UpdateCommentHandler : IRequestHandler<UpdateCommentCommand, CommentDto>
{
	private static readonly TimeSpan EditWindow = TimeSpan.FromMinutes(15);

	private readonly DatabaseContext _dbContext;
	private readonly ICurrentUserAccessor _currentUserAccessor;
	private readonly ICommentContentRenderer _contentRenderer;
	private readonly IDateProvider _dateProvider;
	private readonly ILogger<UpdateCommentHandler> _logger;

	public UpdateCommentHandler(
		DatabaseContext dbContext,
		ICurrentUserAccessor currentUserAccessor,
		ICommentContentRenderer contentRenderer,
		IDateProvider dateProvider,
		ILogger<UpdateCommentHandler> logger)
	{
		_dbContext = dbContext;
		_currentUserAccessor = currentUserAccessor;
		_contentRenderer = contentRenderer;
		_dateProvider = dateProvider;
		_logger = logger;
	}

	public async Task<CommentDto> Handle(UpdateCommentCommand request, CancellationToken cancellationToken)
	{
		var author = _currentUserAccessor.Current
			?? throw new CommentForbiddenException("A signed-in author is required to edit a comment.");

		var content = request.Content.Trim();
		if (content.Length == 0)
		{
			throw new CommentValidationException("Comment content must not be empty.");
		}

		if (content.Length > CommentLimits.MaxCommentLength)
		{
			throw new CommentValidationException(
				$"Comment content must not exceed {CommentLimits.MaxCommentLength} characters.");
		}

		var comment = await _dbContext.Comment
			.Include(c => c.Author)
			.Include(c => c.Votes)
			.FirstOrDefaultAsync(c => c.Id == request.CommentId, cancellationToken)
			?? throw new CommentNotFoundException($"Comment {request.CommentId} was not found.");

		if (comment.AuthorId != author.Id)
		{
			throw new CommentForbiddenException($"Comment {comment.Id} belongs to another author.");
		}

		if (comment.Status != DataCommentStatus.Visible)
		{
			throw new CommentConflictException($"Comment {comment.Id} is not visible and cannot be edited.");
		}

		var now = _dateProvider.UtcNow.UtcDateTime;
		if (now - comment.CreatedOn > EditWindow)
		{
			throw new CommentConflictException(
				$"Comment {comment.Id} can only be edited within {EditWindow.TotalMinutes} minutes of posting.");
		}

		// ParentId and MentionedDisplayName are decided once, at creation. Re-deriving them here
		// would double-prefix a mention or silently re-thread an existing reply.
		comment.Content = content;
		comment.ContentHtml = _contentRenderer.RenderHtml(content);
		comment.EditedOn = now;

		await _dbContext.SaveChangesAsync(cancellationToken);

		_logger.LogInformation("Comment {CommentId} edited by its author.", comment.Id);

		return CommentProjection.ToDto(comment, author.Id, includeReplies: false);
	}
}
