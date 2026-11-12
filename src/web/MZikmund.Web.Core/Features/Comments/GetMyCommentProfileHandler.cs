using MediatR;
using Microsoft.EntityFrameworkCore;
using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Web.Core.Services.Comments;
using MZikmund.Web.Data;

namespace MZikmund.Web.Core.Features.Comments;

internal sealed class GetMyCommentProfileHandler : IRequestHandler<GetMyCommentProfileQuery, CommentProfile>
{
	private readonly DatabaseContext _dbContext;
	private readonly ICurrentUserAccessor _currentUserAccessor;

	public GetMyCommentProfileHandler(DatabaseContext dbContext, ICurrentUserAccessor currentUserAccessor)
	{
		_dbContext = dbContext;
		_currentUserAccessor = currentUserAccessor;
	}

	public async Task<CommentProfile> Handle(GetMyCommentProfileQuery request, CancellationToken cancellationToken)
	{
		var current = _currentUserAccessor.Current
			?? throw new CommentForbiddenException("A signed-in author is required to read the comment profile.");

		var author = await _dbContext.CommentAuthor
			.AsNoTracking()
			.FirstOrDefaultAsync(a => a.Id == current.Id, cancellationToken);

		// Until a name is chosen, the next comment is named from the token, whatever the row last stored.
		var hasChosen = author?.HasChosenDisplayName == true;
		return new CommentProfile
		{
			DisplayName = hasChosen ? author!.DisplayName : current.DisplayName,
			HasChosenDisplayName = hasChosen,
			IsBanned = author?.IsBanned == true,
		};
	}
}
