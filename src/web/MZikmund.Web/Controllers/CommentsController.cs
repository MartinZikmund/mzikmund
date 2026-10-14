using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Web.Core.Features.Comments;
using MZikmund.Web.Core.Services.Comments;
using MZikmund.Web.Filters;
using MZikmund.Web.Infrastructure;

namespace MZikmund.Web.Controllers;

/// <summary>
/// Public API endpoints for post comments.
/// </summary>
/// <remarks>
/// This is the only controller in the solution mixing anonymous and authenticated actions:
/// [Authorize] sits at class level and the two read actions opt out with [AllowAnonymous].
/// </remarks>
[ApiController]
[Authorize]
[Route("api/v1")]
[ServiceFilter(typeof(CommentsEnabledFilter))]
[CommentsExceptionFilter]
public class CommentsController : Controller
{
	/// <summary>
	/// Header carrying the Cloudflare Turnstile token on web submissions.
	/// </summary>
	public const string TurnstileHeaderName = "CF-Turnstile-Response";

	private const int PageSize = 20;
	private const int RepliesPageSize = 20;

	private readonly IMediator _mediator;
	private readonly ITurnstileVerifier _turnstileVerifier;
	private readonly ILogger<CommentsController> _logger;

	/// <summary>
	/// Initializes a new instance of the <see cref="CommentsController"/> class.
	/// </summary>
	/// <param name="mediator">Mediator.</param>
	/// <param name="turnstileVerifier">Cloudflare Turnstile verifier.</param>
	/// <param name="logger">Logger.</param>
	public CommentsController(
		IMediator mediator,
		ITurnstileVerifier turnstileVerifier,
		ILogger<CommentsController> logger)
	{
		_mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
		_turnstileVerifier = turnstileVerifier ?? throw new ArgumentNullException(nameof(turnstileVerifier));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	/// <summary>
	/// Gets top-level comments for a post, each carrying its first replies.
	/// </summary>
	/// <param name="postId">Post ID.</param>
	/// <param name="sort">Sort order.</param>
	/// <param name="pageNumber">Page number (1-based).</param>
	/// <returns>A page of top-level comments.</returns>
	[HttpGet("posts/{postId}/comments")]
	[AllowAnonymous]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> GetPostComments(
		Guid postId,
		[FromQuery] CommentSortOrder sort = CommentSortOrder.Top,
		[FromQuery] int pageNumber = 1) =>
		Ok(await _mediator.Send(new GetPostCommentsQuery(postId, sort, pageNumber, PageSize)));

	/// <summary>
	/// Gets the replies of a comment beyond the first few returned inline.
	/// </summary>
	/// <param name="id">Parent comment ID.</param>
	/// <param name="pageNumber">Page number (1-based).</param>
	/// <returns>A page of replies, oldest first.</returns>
	[HttpGet("comments/{id}/replies")]
	[AllowAnonymous]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> GetCommentReplies(Guid id, [FromQuery] int pageNumber = 1) =>
		Ok(await _mediator.Send(new GetCommentRepliesQuery(id, pageNumber, RepliesPageSize)));

	/// <summary>
	/// Creates a comment or a reply on a post.
	/// </summary>
	/// <param name="postId">Post ID.</param>
	/// <param name="request">Comment content and optional parent.</param>
	/// <param name="turnstileToken">Cloudflare Turnstile token, when the caller is the web form.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The created comment.</returns>
	[HttpPost("posts/{postId}/comments")]
	[EnableRateLimiting(RateLimitPolicies.CommentCreate)]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
	public async Task<IActionResult> CreateComment(
		Guid postId,
		[FromBody] CreateCommentRequest request,
		[FromHeader(Name = TurnstileHeaderName)] string? turnstileToken,
		CancellationToken cancellationToken)
	{
		if (!await VerifyTurnstileAsync(turnstileToken, cancellationToken))
		{
			return BadRequest(new
			{
				error = "Captcha verification failed. Please try again."
			});
		}

		return Ok(await _mediator.Send(
			new CreateCommentCommand(postId, request.Content, request.ParentId),
			cancellationToken));
	}

	/// <summary>
	/// Edits the caller's own comment, within the allowed edit window.
	/// </summary>
	/// <param name="id">Comment ID.</param>
	/// <param name="request">New content.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The updated comment.</returns>
	[HttpPatch("comments/{id}")]
	[EnableRateLimiting(RateLimitPolicies.CommentEdit)]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status409Conflict)]
	[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
	public async Task<IActionResult> UpdateComment(
		Guid id,
		[FromBody] UpdateCommentRequest request,
		CancellationToken cancellationToken) =>
		Ok(await _mediator.Send(new UpdateCommentCommand(id, request.Content), cancellationToken));

	/// <summary>
	/// Soft-deletes the caller's own comment, leaving a tombstone so replies survive.
	/// </summary>
	/// <param name="id">Comment ID.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>No content, when successful.</returns>
	[HttpDelete("comments/{id}")]
	[EnableRateLimiting(RateLimitPolicies.CommentEdit)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
	public async Task<IActionResult> DeleteComment(Guid id, CancellationToken cancellationToken)
	{
		await _mediator.Send(new DeleteCommentCommand(id), cancellationToken);
		return NoContent();
	}

	/// <summary>
	/// Upvotes a comment. Idempotent.
	/// </summary>
	/// <param name="id">Comment ID.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The comment with refreshed vote state.</returns>
	[HttpPut("comments/{id}/vote")]
	[EnableRateLimiting(RateLimitPolicies.CommentVote)]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
	public async Task<IActionResult> AddVote(Guid id, CancellationToken cancellationToken) =>
		Ok(await _mediator.Send(new SetCommentVoteCommand(id, true), cancellationToken));

	/// <summary>
	/// Removes the caller's upvote from a comment. Idempotent.
	/// </summary>
	/// <param name="id">Comment ID.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The comment with refreshed vote state.</returns>
	[HttpDelete("comments/{id}/vote")]
	[EnableRateLimiting(RateLimitPolicies.CommentVote)]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
	public async Task<IActionResult> RemoveVote(Guid id, CancellationToken cancellationToken) =>
		Ok(await _mediator.Send(new SetCommentVoteCommand(id, false), cancellationToken));

	/// <summary>
	/// Reports a comment for moderation.
	/// </summary>
	/// <param name="id">Comment ID.</param>
	/// <param name="request">Report reason and optional note.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>No content, when successful.</returns>
	[HttpPost("comments/{id}/report")]
	[EnableRateLimiting(RateLimitPolicies.CommentReport)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
	public async Task<IActionResult> ReportComment(
		Guid id,
		[FromBody] ReportCommentRequest request,
		CancellationToken cancellationToken)
	{
		await _mediator.Send(new ReportCommentCommand(id, request.Reason, request.Note), cancellationToken);
		return NoContent();
	}

	/// <summary>
	/// Enforcement predicate: a token that is present MUST verify; an absent token is permitted
	/// and logged, because the Uno app cannot render a Turnstile widget.
	/// </summary>
	private async Task<bool> VerifyTurnstileAsync(string? token, CancellationToken cancellationToken)
	{
		var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();

		if (string.IsNullOrEmpty(token))
		{
			if (_logger.IsEnabled(LogLevel.Information))
			{
				_logger.LogInformation("Comment write accepted without a Turnstile token from {RemoteIp}.", remoteIp);
			}

			return true;
		}

		var verified = await _turnstileVerifier.VerifyAsync(token, remoteIp, cancellationToken);
		if (!verified && _logger.IsEnabled(LogLevel.Warning))
		{
			_logger.LogWarning("Turnstile verification failed for a comment write from {RemoteIp}.", remoteIp);
		}

		return verified;
	}
}
