using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Web.Core.Services;
using MZikmund.Web.Core.Services.Comments;
using MZikmund.Web.Data;
using MZikmund.Web.Data.Entities;

namespace MZikmund.Web.Core.Infrastructure;

/// <summary>
/// Upserts the comment author row from the authenticated caller's claims and rejects banned
/// authors, for every request marked with <see cref="IAuthoredRequest"/>.
/// </summary>
/// <remarks>
/// Registered as an open behavior. The generic constraint is honoured by the built-in DI container,
/// which skips open generic implementations whose constraints a request type does not satisfy, so
/// unmarked requests never see this behavior. The author upsert is saved before the handler runs
/// and its transaction, so it is not rolled back if the write itself fails - acceptable by design.
/// </remarks>
public sealed class AuthoredRequestBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
	where TRequest : IAuthoredRequest
{
	private readonly DatabaseContext _dbContext;
	private readonly ICurrentUserAccessor _currentUserAccessor;
	private readonly IDateProvider _dateProvider;
	private readonly ILogger<AuthoredRequestBehavior<TRequest, TResponse>> _logger;

	public AuthoredRequestBehavior(
		DatabaseContext dbContext,
		ICurrentUserAccessor currentUserAccessor,
		IDateProvider dateProvider,
		ILogger<AuthoredRequestBehavior<TRequest, TResponse>> logger)
	{
		_dbContext = dbContext;
		_currentUserAccessor = currentUserAccessor;
		_dateProvider = dateProvider;
		_logger = logger;
	}

	public async Task<TResponse> Handle(
		TRequest request,
		RequestHandlerDelegate<TResponse> next,
		CancellationToken cancellationToken)
	{
		var current = _currentUserAccessor.Current;
		if (current is null)
		{
			throw new CommentForbiddenException(
				$"{typeof(TRequest).Name} requires an authenticated author.");
		}

		var now = _dateProvider.UtcNow.UtcDateTime;

		var author = await _dbContext.CommentAuthor
			.FirstOrDefaultAsync(a => a.Id == current.Id, cancellationToken);

		var inserted = false;
		if (author is null)
		{
			author = new CommentAuthorEntity
			{
				Id = current.Id,
				CreatedOn = now,
				NotifyOnReply = true
			};

			ApplyProfile(author, current, now);
			_dbContext.CommentAuthor.Add(author);

			try
			{
				await _dbContext.SaveChangesAsync(cancellationToken);
				inserted = true;
				_logger.LogInformation("Created comment author projection for {AuthorId}.", current.Id);
			}
			catch (DbUpdateException)
			{
				// Two first requests from the same new user raced and the other one won the insert:
				// carry on with the row that now exists.
				_dbContext.ChangeTracker.Clear();
				author = await _dbContext.CommentAuthor
					.FirstOrDefaultAsync(a => a.Id == current.Id, cancellationToken);
				if (author is null)
				{
					throw;
				}
			}
		}

		if (!inserted)
		{
			if (author.IsBanned && request is not IAllowsBannedAuthor)
			{
				// Bans block future writes only; existing comments stay visible.
				throw new CommentForbiddenException($"Author {current.Id} is banned.", CommentErrorCodes.Banned);
			}

			ApplyProfile(author, current, now);
			await _dbContext.SaveChangesAsync(cancellationToken);
		}

		return await next(cancellationToken);
	}

	private static void ApplyProfile(CommentAuthorEntity author, CommentAuthorInfo current, DateTime now)
	{
		author.DisplayName = current.DisplayName;
		author.AvatarUrl = current.AvatarUrl;
		author.Email = current.Email;
		author.IsAdmin = current.IsAdmin;
		author.LastSeenOn = now;
	}
}
