using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Web.Core.Services;
using MZikmund.Web.Core.Services.Comments;
using MZikmund.Web.Data;
using MZikmund.Web.Data.Entities;

namespace MZikmund.Web.Core.Features.Comments;

internal sealed class UpdateMyCommentProfileHandler : IRequestHandler<UpdateMyCommentProfileCommand, CommentProfile>
{
	private readonly DatabaseContext _dbContext;
	private readonly ICurrentUserAccessor _currentUserAccessor;
	private readonly IDateProvider _dateProvider;
	private readonly ILogger<UpdateMyCommentProfileHandler> _logger;

	public UpdateMyCommentProfileHandler(
		DatabaseContext dbContext,
		ICurrentUserAccessor currentUserAccessor,
		IDateProvider dateProvider,
		ILogger<UpdateMyCommentProfileHandler> logger)
	{
		_dbContext = dbContext;
		_currentUserAccessor = currentUserAccessor;
		_dateProvider = dateProvider;
		_logger = logger;
	}

	public async Task<CommentProfile> Handle(UpdateMyCommentProfileCommand request, CancellationToken cancellationToken)
	{
		var current = _currentUserAccessor.Current
			?? throw new CommentForbiddenException("A signed-in author is required to set a display name.");

		var check = DisplayNameRules.Check(request.DisplayName, current.IsAdmin);
		if (check.IsReserved)
		{
			throw new CommentValidationException(
				$"Display name for {current.Id} is reserved.",
				CommentErrorCodes.DisplayNameReserved);
		}

		if (check.IsInvalid)
		{
			throw new CommentValidationException(
				$"Display name for {current.Id} is invalid: {check.Problem}.",
				CommentErrorCodes.DisplayNameInvalid,
				InvalidDetail(check.Problem));
		}

		var author = await _dbContext.CommentAuthor
			.FirstOrDefaultAsync(a => a.Id == current.Id, cancellationToken);

		if (author is null)
		{
			var now = _dateProvider.UtcNow.UtcDateTime;
			author = new CommentAuthorEntity
			{
				Id = current.Id,
				CreatedOn = now,
				LastSeenOn = now,
				NotifyOnReply = true,
				AvatarUrl = current.AvatarUrl,
				Email = current.Email,
				IsAdmin = current.IsAdmin,
			};
			_dbContext.CommentAuthor.Add(author);
		}
		else if (author.IsBanned)
		{
			throw new CommentForbiddenException($"Author {current.Id} is banned.", CommentErrorCodes.Banned);
		}

		author.DisplayName = check.Name;
		author.HasChosenDisplayName = true;
		await _dbContext.SaveChangesAsync(cancellationToken);

		_logger.LogInformation("Comment author {AuthorId} chose a display name.", author.Id);

		return new CommentProfile
		{
			DisplayName = author.DisplayName,
			HasChosenDisplayName = true,
			IsBanned = false,
		};
	}

	private static string InvalidDetail(DisplayNameProblem problem) => problem switch
	{
		DisplayNameProblem.TooShort => $"A name needs at least {DisplayNameRules.MinLength} characters.",
		DisplayNameProblem.TooLong => $"A name can have at most {DisplayNameRules.MaxLength} characters.",
		_ => "A name needs at least one letter or digit.",
	};
}
