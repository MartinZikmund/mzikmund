namespace MZikmund.DataContracts.Videos;

/// <summary>
/// Represents a YouTube video from the channel RSS feed.
/// </summary>
public record VideoDto
{
	/// <summary>
	/// YouTube video ID (extracted from URL).
	/// Example: "dQw4w9WgXcQ"
	/// </summary>
	public required string VideoId { get; init; }

	/// <summary>
	/// Video title from RSS feed.
	/// Example: "Building APIs with .NET 10"
	/// </summary>
	public required string Title { get; init; }

	/// <summary>
	/// Video description from RSS feed (HTML entities decoded, trimmed to 3-4 lines).
	/// Example: "In this video, we explore the latest features..."
	/// </summary>
	public required string Description { get; init; }

	/// <summary>
	/// URL to YouTube video thumbnail (16:9 aspect ratio).
	/// Provided by YouTube; HTTPS-accessible.
	/// Example: "https://i.ytimg.com/vi/dQw4w9WgXcQ/maxresdefault.jpg"
	/// </summary>
	public required string ThumbnailUrl { get; init; }

	/// <summary>
	/// Publication date/time from YouTube RSS feed.
	/// RFC 3339 format in feed, converted to DateTimeOffset.
	/// Example: 2025-02-10T14:30:00Z
	/// </summary>
	public required DateTimeOffset PublishedDate { get; init; }

	/// <summary>
	/// Direct YouTube video URL.
	/// Example: "https://www.youtube.com/watch?v=dQw4w9WgXcQ"
	/// </summary>
	public required string YouTubeUrl { get; init; }

	/// <summary>
	/// YouTube channel ID (for reference; from feed metadata).
	/// Example: "UCB6Td35bzTvcJN_HG6TLtwA"
	/// </summary>
	public string? ChannelId { get; init; }
}
