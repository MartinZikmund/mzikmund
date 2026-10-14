using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MZikmund.Web.Core.Features.Comments;
using MZikmund.Web.Filters;

namespace MZikmund.Web.Controllers.Admin;

/// <summary>
/// Admin moderation operations for comments.
/// </summary>
/// <remarks>
/// Deliberately NOT decorated with the Comments:Enabled filter — moderation must stay reachable
/// while the public feature is dark so production can be smoke-tested.
/// </remarks>
[ApiController]
[Authorize(Policy = "AdminPolicy")]
[Route("api/v1/admin/comments")]
[CommentsExceptionFilter]
public class CommentsAdminController : Controller
{
	private const int PageSize = 20;
	private readonly IMediator _mediator;

	/// <summary>
	/// Initializes a new instance of the <see cref="CommentsAdminController"/> class.
	/// </summary>
	/// <param name="mediator">Mediator.</param>
	public CommentsAdminController(IMediator mediator) =>
		_mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

	/// <summary>
	/// Normalizes a bound instant to UTC. Model binding yields Local for a value with an offset;
	/// a value with no offset at all is Unspecified, which ToUniversalTime would wrongly read as
	/// server-local time, so it is taken as UTC.
	/// </summary>
	private static DateTime? ToUtc(DateTime? value) => value switch
	{
		null => null,
		{ Kind: DateTimeKind.Unspecified } unspecified => DateTime.SpecifyKind(unspecified, DateTimeKind.Utc),
		{ } instant => instant.ToUniversalTime(),
	};

	/// <summary>
	/// Gets the open report queue, grouped by comment.
	/// </summary>
	/// <param name="pageNumber">Page number (1-based).</param>
	/// <returns>A page of reported comments.</returns>
	[HttpGet("reports")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	public async Task<IActionResult> GetReports([FromQuery] int pageNumber = 1) =>
		Ok(await _mediator.Send(new GetCommentReportsQuery(pageNumber, PageSize)));

	/// <summary>
	/// Dismisses every open report on a comment without moderating the comment itself.
	/// </summary>
	/// <param name="commentId">Comment ID.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>No content, when successful.</returns>
	[HttpPost("reports/{commentId}/resolve")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> ResolveReport(Guid commentId, CancellationToken cancellationToken)
	{
		await _mediator.Send(new ResolveCommentReportCommand(commentId), cancellationToken);
		return NoContent();
	}

	/// <summary>
	/// Gets the newest-first moderation feed.
	/// </summary>
	/// <param name="since">Only comments created after this instant. Pass an ISO 8601 UTC instant; one without an offset is read as UTC.</param>
	/// <param name="pageNumber">Page number (1-based).</param>
	/// <returns>A page of comments, newest first.</returns>
	[HttpGet("")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	public async Task<IActionResult> GetModerationFeed(
		[FromQuery] DateTime? since = null,
		[FromQuery] int pageNumber = 1) =>
		Ok(await _mediator.Send(new GetModerationFeedQuery(ToUtc(since), pageNumber, PageSize)));

	/// <summary>
	/// Counts comments created since the given instant, for the app's unread badge.
	/// </summary>
	/// <param name="since">Only comments created after this instant.</param>
	/// <returns>The number of new comments.</returns>
	[HttpGet("count")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	public async Task<IActionResult> CountNewComments([FromQuery] DateTime? since = null) =>
		Ok(await _mediator.Send(new CountNewCommentsQuery(ToUtc(since))));

	/// <summary>
	/// Hides a comment from public view and auto-resolves its open reports.
	/// </summary>
	/// <param name="id">Comment ID.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>No content, when successful.</returns>
	[HttpPost("{id}/hide")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> HideComment(Guid id, CancellationToken cancellationToken)
	{
		await _mediator.Send(new HideCommentCommand(id), cancellationToken);
		return NoContent();
	}

	/// <summary>
	/// Deletes a comment as a moderator and auto-resolves its open reports.
	/// </summary>
	/// <param name="id">Comment ID.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>No content, when successful.</returns>
	[HttpDelete("{id}")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> DeleteComment(Guid id, CancellationToken cancellationToken)
	{
		await _mediator.Send(new AdminDeleteCommentCommand(id), cancellationToken);
		return NoContent();
	}

	/// <summary>
	/// Bans a comment author from future writes. Existing comments are left visible.
	/// </summary>
	/// <param name="id">Author ID.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>No content, when successful.</returns>
	/// <remarks>
	/// The leading <c>~/</c> escapes the class-level <c>api/v1/admin/comments</c> prefix so the
	/// route matches the contract exactly.
	/// </remarks>
	[HttpPost("~/api/v1/admin/authors/{id}/ban")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> BanAuthor(Guid id, CancellationToken cancellationToken)
	{
		await _mediator.Send(new BanAuthorCommand(id), cancellationToken);
		return NoContent();
	}
}
