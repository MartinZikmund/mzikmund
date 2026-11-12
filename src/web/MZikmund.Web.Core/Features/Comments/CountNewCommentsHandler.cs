using MediatR;
using Microsoft.EntityFrameworkCore;
using MZikmund.Web.Data;

namespace MZikmund.Web.Core.Features.Comments;

internal sealed class CountNewCommentsHandler : IRequestHandler<CountNewCommentsQuery, int>
{
	private readonly DatabaseContext _dbContext;

	public CountNewCommentsHandler(DatabaseContext dbContext)
	{
		_dbContext = dbContext;
	}

	public Task<int> Handle(CountNewCommentsQuery request, CancellationToken cancellationToken) =>
		CommentModerationFeed.Since(_dbContext, request.Since).CountAsync(cancellationToken);
}
