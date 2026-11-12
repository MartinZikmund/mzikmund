using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MZikmund.Web.Core.Services.Comments;
using MZikmund.Web.Data;

namespace MZikmund.Web.Core.Features.Comments;

internal sealed class BanAuthorHandler : IRequestHandler<BanAuthorCommand>
{
	private readonly DatabaseContext _dbContext;
	private readonly ILogger<BanAuthorHandler> _logger;

	public BanAuthorHandler(DatabaseContext dbContext, ILogger<BanAuthorHandler> logger)
	{
		_dbContext = dbContext;
		_logger = logger;
	}

	public async Task Handle(BanAuthorCommand request, CancellationToken cancellationToken)
	{
		var author = await _dbContext.CommentAuthor
			.FirstOrDefaultAsync(a => a.Id == request.AuthorId, cancellationToken)
			?? throw new CommentNotFoundException($"Comment author {request.AuthorId} was not found.");

		if (author.IsBanned)
		{
			return;
		}

		// Bans block future writes only; existing comments are never touched here.
		author.IsBanned = true;
		await _dbContext.SaveChangesAsync(cancellationToken);

		_logger.LogInformation("Comment author {AuthorId} banned.", author.Id);
	}
}
