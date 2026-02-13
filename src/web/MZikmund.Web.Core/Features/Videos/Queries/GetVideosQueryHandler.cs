using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MZikmund.DataContracts.Videos;
using MZikmund.Web.Configuration;
using MZikmund.Web.Core.Features.Videos.RssParsing;
using MZikmund.Web.Core.Services;

namespace MZikmund.Web.Core.Features.Videos.Queries;

/// <summary>
/// Handles GetVideosQuery: fetches from YouTube RSS feed with 30-minute caching.
/// </summary>
public class GetVideosQueryHandler : IRequestHandler<GetVideosQuery, List<VideoDto>?>
{
	private readonly ILogger<GetVideosQueryHandler> _logger;
	private readonly ICache _cache;
	private readonly YouTubeRssFeedParser _parser;
	private readonly IOptions<YouTubeOptions> _options;

	public GetVideosQueryHandler(
		ILogger<GetVideosQueryHandler> logger,
		ICache cache,
		YouTubeRssFeedParser parser,
		IOptions<YouTubeOptions> options)
	{
		_logger = logger;
		_cache = cache;
		_parser = parser;
		_options = options;
	}

	public async Task<List<VideoDto>?> Handle(GetVideosQuery request, CancellationToken cancellationToken)
	{
		var cacheKey = request.Count.HasValue
			? $"cache:videos:latest:{request.Count}"
			: "cache:videos:all";

		// Try cache first
		var cached = await _cache.GetAsync<List<VideoDto>>(cacheKey, cancellationToken);
		if (cached is not null)
		{
			_logger.LogInformation($"Retrieved {cached.Count} videos from cache (key: {cacheKey})");
			return cached;
		}

		// Cache miss: fetch fresh data
		try
		{
			_logger.LogInformation("Cache miss; fetching fresh videos from YouTube RSS feed");
			var feed = await _parser.ParseAsync(_options.Value.FeedUrl, cancellationToken);

			// Apply count limit if specified
			var result = request.Count.HasValue
				? feed.Take(request.Count.Value).ToList()
				: feed;

			// Cache for 30 minutes
			var ttl = TimeSpan.FromMinutes(_options.Value.CacheTtlMinutes);
			await _cache.SetAsync(cacheKey, result, ttl, cancellationToken);

			_logger.LogInformation($"Cached {result.Count} videos with {_options.Value.CacheTtlMinutes}-minute TTL");
			return result;
		}
		catch (Exception ex)
		{
			_logger.LogError($"Failed to fetch YouTube videos: {ex.Message}");
			return null;  // Let controller convert to 503 error
		}
	}
}
