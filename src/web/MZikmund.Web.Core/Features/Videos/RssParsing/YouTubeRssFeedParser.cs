using System.ServiceModel.Syndication;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using MZikmund.DataContracts.Videos;
using Microsoft.Extensions.Logging;

namespace MZikmund.Web.Core.Features.Videos.RssParsing;

/// <summary>
/// Parses YouTube RSS feed and converts to VideoDto objects.
/// </summary>
public class YouTubeRssFeedParser
{
	private readonly ILogger<YouTubeRssFeedParser> _logger;
	private readonly IHttpClientFactory _httpClientFactory;

	public YouTubeRssFeedParser(ILogger<YouTubeRssFeedParser> logger, IHttpClientFactory httpClientFactory)
	{
		_logger = logger;
		_httpClientFactory = httpClientFactory;
	}

	/// <summary>
	/// Parses YouTube RSS feed from the specified URL.
	/// </summary>
	/// <param name="feedUrl">The YouTube RSS feed URL.</param>
	/// <param name="ct">Cancellation token.</param>
	/// <returns>List of VideoDto objects in reverse chronological order.</returns>
	public async Task<List<VideoDto>> ParseAsync(string feedUrl, CancellationToken ct = default)
	{
		try
		{
			var videos = new List<VideoDto>();

			using var httpClient = _httpClientFactory.CreateClient("YouTube");
			using (var httpStream = await httpClient.GetStreamAsync(feedUrl, ct))
			using (var xmlReader = XmlReader.Create(httpStream))
			{
				var feed = SyndicationFeed.Load(xmlReader);

				foreach (var item in feed.Items)
				{
					try
					{
						var video = ParseFeedItem(item);
						if (video is not null)
						{
							videos.Add(video);
						}
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "Skipping malformed video entry");
					}
				}

				// Sort by date descending (newest first)
				videos = videos
					.OrderByDescending(v => v.PublishedDate)
					.ToList();

				_logger.LogInformation("Successfully parsed {Count} videos from YouTube RSS feed", videos.Count);
				return videos;
			}
		}
		catch (HttpRequestException ex)
		{
			_logger.LogError(ex, "Failed to fetch YouTube RSS feed");
			throw;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error parsing YouTube RSS feed");
			throw;
		}
	}

	private static readonly XNamespace MediaNs = "http://search.yahoo.com/mrss/";

	/// <summary>
	/// Parses a single SyndicationItem from YouTube RSS feed.
	/// </summary>
	private static VideoDto? ParseFeedItem(SyndicationItem item)
	{
		// Extract video ID from link
		if (item.Links.FirstOrDefault(l => l.RelationshipType == "alternate")?.Uri is not { } videoUri)
			return null;

		var videoUrl = videoUri.AbsoluteUri;
		var videoId = ExtractVideoId(videoUrl);
		if (string.IsNullOrEmpty(videoId))
			return null;

		// Try to find media:group element (YouTube nests media:thumbnail and media:description inside it)
		XElement? mediaGroup = null;
		var groupExt = item.ElementExtensions.FirstOrDefault(e => e.OuterName == "group" && e.OuterNamespace == MediaNs.NamespaceName);
		if (groupExt is not null)
		{
			using var reader = groupExt.GetReader();
			mediaGroup = (XElement)XNode.ReadFrom(reader);
		}

		// Extract thumbnail: media:group → direct extension → video-ID-based fallback
		var thumbnailUrl = mediaGroup?.Element(MediaNs + "thumbnail")?.Attribute("url")?.Value
			?? item.ElementExtensions.FirstOrDefault(e => e.OuterName == "thumbnail")?.GetReader()?.GetAttribute("url")
			?? $"https://i.ytimg.com/vi/{videoId}/mqdefault.jpg";

		// Extract description: media:group → direct extension → summary → empty
		var description = mediaGroup?.Element(MediaNs + "description")?.Value
			?? item.ElementExtensions.FirstOrDefault(e => e.OuterName == "description")?.GetReader()?.ReadElementContentAsString()
			?? (item.Summary is TextSyndicationContent textSummary ? textSummary.Text : item.Summary?.Text) ?? "";

		description = System.Net.WebUtility.HtmlDecode(description);
		description = TrimDescription(description, 3);  // Trim to 3-4 lines

		// Validate thumbnail URL
		if (!Uri.TryCreate(thumbnailUrl, UriKind.Absolute, out var thumbnailUri) ||
			thumbnailUri.Scheme != "https")
		{
			thumbnailUrl = $"https://i.ytimg.com/vi/{videoId}/mqdefault.jpg";
		}

		return new VideoDto
		{
			VideoId = videoId,
			Title = item.Title?.Text ?? "",
			Description = description,
			ThumbnailUrl = thumbnailUrl,
			PublishedDate = item.PublishDate.ToUniversalTime(),
			YouTubeUrl = videoUrl,
			ChannelId = item.Authors.FirstOrDefault()?.Name
		};
	}

	/// <summary>
	/// Extracts the video ID from a YouTube URL.
	/// Expected format: https://www.youtube.com/watch?v=VIDEO_ID
	/// </summary>
	private static string? ExtractVideoId(string youtubeUrl)
	{
		try
		{
			var uri = new Uri(youtubeUrl);
			return HttpUtility.ParseQueryString(uri.Query)["v"];
		}
		catch
		{
			return null;
		}
	}

	/// <summary>
	/// Trims description to approximately N lines (~50 chars per line).
	/// </summary>
	private static string TrimDescription(string text, int maxLines = 3)
	{
		const int charsPerLine = 50;
		var maxChars = charsPerLine * maxLines;

		if (text.Length <= maxChars)
			return text;

		var trimmed = text[..maxChars];
		var lastSpace = trimmed.LastIndexOf(' ');

		return lastSpace > 0 ? trimmed[..lastSpace] + "…" : trimmed + "…";
	}
}
