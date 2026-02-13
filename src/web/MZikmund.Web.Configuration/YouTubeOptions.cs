namespace MZikmund.Web.Configuration;

/// <summary>
/// Configuration options for YouTube RSS feed integration.
/// Loaded from appsettings.json [YouTube] section.
/// </summary>
public class YouTubeOptions
{
	/// <summary>
	/// RSS feed URL for the YouTube channel.
	/// Format: https://www.youtube.com/feeds/videos.xml?channel_id={CHANNEL_ID}
	/// </summary>
	public string FeedUrl { get; set; } = "";

	/// <summary>
	/// YouTube channel URL for "View all videos" / "Subscribe" links.
	/// Format: https://www.youtube.com/@{HANDLE}
	/// </summary>
	public string ChannelUrl { get; set; } = "";

	/// <summary>
	/// Cache time-to-live in minutes (FR-001: 30 minutes minimum).
	/// </summary>
	public int CacheTtlMinutes { get; set; }
}
