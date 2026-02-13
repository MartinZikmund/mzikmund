using MediatR;
using Microsoft.AspNetCore.Mvc;
using MZikmund.Web.Core.Features.Videos.Queries;

namespace MZikmund.Web.Controllers;

/// <summary>
/// API endpoints for YouTube videos.
/// </summary>
[ApiController]
[Route("api/v1/videos")]
public class VideosController : ControllerBase
{
	private readonly IMediator _mediator;
	private readonly ILogger<VideosController> _logger;

	public VideosController(IMediator mediator, ILogger<VideosController> logger)
	{
		_mediator = mediator;
		_logger = logger;
	}

	/// <summary>
	/// Gets all available videos from the YouTube RSS feed.
	/// Results are cached for 30 minutes.
	/// </summary>
	/// <returns>List of video DTOs in reverse chronological order.</returns>
	[HttpGet]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
	public async Task<IActionResult> GetVideos()
	{
		var videos = await _mediator.Send(new GetVideosQuery());

		if (videos is null)
		{
			_logger.LogWarning("YouTube RSS feed unavailable");
			return StatusCode(503, new
			{
				error = "YouTube feed temporarily unavailable. Please try again later.",
				timestamp = DateTime.UtcNow
			});
		}

		return Ok(videos);
	}

	/// <summary>
	/// Gets the N most recent videos from the YouTube RSS feed.
	/// Used for home page featured section (typically count=3).
	/// Results are cached for 30 minutes.
	/// </summary>
	/// <param name="count">Number of videos to return (1-10).</param>
	/// <returns>List of up to N most recent video DTOs.</returns>
	[HttpGet("latest")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
	public async Task<IActionResult> GetLatestVideos([FromQuery] int? count = null)
	{
		// Validate count parameter
		if (count.HasValue && (count < 1 || count > 10))
		{
			return BadRequest(new
			{
				error = "Invalid count parameter. Must be integer between 1 and 10."
			});
		}

		var videos = await _mediator.Send(new GetVideosQuery { Count = count ?? 3 });

		if (videos is null)
		{
			return StatusCode(503, new
			{
				error = "YouTube feed temporarily unavailable. Please try again later.",
				timestamp = DateTime.UtcNow
			});
		}

		return Ok(videos);
	}
}
