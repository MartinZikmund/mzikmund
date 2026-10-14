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
		var comment = await _dbContext.Comment
			.FirstOrDefaultAsync(c => c.Id == request.CommentId, cancellationToken)
			?? throw new CommentNotFoundException($"Comment {request.CommentId} was not found.");

		var resolvedReports = await CommentModeration.ResolveOpenReportsAsync(
			_dbContext, comment, _dateProvider.UtcNow.UtcDateTime, cancellationToken);
		await CommentModeration.HideAsync(_dbContext, comment, cancellationToken);

		// The row stays so replies keep their parent; only the text is purged.
		comment.Content = "";
		comment.ContentHtml = "";

		await _dbContext.SaveChangesAsync(cancellationToken);

		_logger.LogInformation(
			"Comment {CommentId} deleted by moderator; {ResolvedReports} open report(s) auto-resolved.",
			comment.Id,
			resolvedReports);
	}
}
