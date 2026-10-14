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
		var comment = await _dbContext.Comment
			.FirstOrDefaultAsync(c => c.Id == request.CommentId, cancellationToken)
			?? throw new CommentNotFoundException($"Comment {request.CommentId} was not found.");

		var resolvedReports = await CommentModeration.ResolveOpenReportsAsync(
			_dbContext, comment, _dateProvider.UtcNow.UtcDateTime, cancellationToken);
		await CommentModeration.HideAsync(_dbContext, comment, cancellationToken);

		// One SaveChangesAsync is one transaction: the queue entry cannot outlive the hide.
		await _dbContext.SaveChangesAsync(cancellationToken);

		_logger.LogInformation(
			"Comment {CommentId} hidden by moderator; {ResolvedReports} open report(s) auto-resolved.",
			comment.Id,
			resolvedReports);
	}
}
