# Data Model: YouTube Videos Integration

**Feature**: 001-youtube-videos | **Date**: February 13, 2025

## Overview

YouTube Videos are read-only, externally-sourced entities fetched from YouTube RSS feed in real-time. No database persistence needed. In-memory caching only.

---

## Entity: Video

**Purpose**: Represents a single YouTube video entry parsed from YouTube RSS feed

**Source**: YouTube RSS feed (`https://www.youtube.com/feeds/videos.xml?channel_id=...`)

**Lifetime**: Fetched on demand, cached in-memory for 30 minutes (FR-001, FR-009)

### DTO Definition

**Location**: `src/shared/MZikmund.DataContracts/Videos/VideoDto.cs`

```csharp
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
```

### Field Specifications

| Field | Type | Required | Source | Constraints | Notes |
|-------|------|----------|--------|-------------|-------|
| `VideoId` | string | Yes | URL extraction | Non-empty, 11 chars | YouTube video IDs are 11 alphanumeric chars |
| `Title` | string | Yes | RSS `<title>` | Non-empty, <200 chars | Directly from feed |
| `Description` | string | Yes | RSS `<description>` | Non-empty, trimmed to ~300 chars (3-4 lines) | HTML entities decoded; trim per FR-007 |
| `ThumbnailUrl` | string | Yes | RSS `<media:thumbnail>` | Valid HTTPS URL | Provided by YouTube; validate URI scheme |
| `PublishedDate` | DateTimeOffset | Yes | RSS `<published>` | RFC 3339 format | Used for reverse chronological sort |
| `YouTubeUrl` | string | Yes | RSS `<link>` | Valid HTTPS URL to youtube.com | Validate URI; scheme + domain check |
| `ChannelId` | string | No | RSS `<yt:channelId>` | Non-empty if present | For attribution; optional |

### Data Constraints

1. **Title**:
   - Max 200 characters (YouTube limit ~100, but allow buffer)
   - Non-empty
   - No validation needed; directly from YouTube

2. **Description**:
   - Trimmed to 3-4 lines max (FR-007)
   - ~300 characters (~50 chars/line × 3-4 lines)
   - HTML entities decoded before storage
   - No markdown processing (unlike blog posts); plain text
   - Validation: Check decoded length; skip if empty after trim

3. **ThumbnailUrl**:
   - Must be valid HTTPS URL
   - Validate: `Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == "https"`
   - Fallback: Use placeholder if missing (per edge case in spec)

4. **PublishedDate**:
   - RFC 3339 format from YouTube
   - Parse: `DateTimeOffset.Parse(xmlElement.Value)`
   - Used for sorting; must be valid

5. **YouTubeUrl**:
   - Format: `https://www.youtube.com/watch?v={VideoId}`
   - Validate: `Uri.TryCreate()` + scheme check + domain check
   - No fragments or additional params in RSS

6. **VideoId**:
   - Extracted from YouTubeUrl query string: `v` parameter
   - 11 alphanumeric characters
   - Extract: `var id = new Uri(url).Query.Split('=')[1]` (or parse properly)

### Relationships

- **Video ↔ Cache**: In-memory cache via `ICache` service
  - Key: `"cache:videos:all"`
  - TTL: 30 minutes (FR-001, FR-009)
  - Backing: No database relationship; external source only

- **Video ↔ YouTube**: Read-only reference to YouTube RSS feed
  - No foreign keys or back-references
  - Feed URL configured in `appsettings.json`

---

## Configuration

**File**: `appsettings.json` (or environment-specific overrides)

```json
{
  "YouTube": {
    "FeedUrl": "https://www.youtube.com/feeds/videos.xml?channel_id=UCB6Td35bzTvcJN_HG6TLtwA",
    "ChannelUrl": "https://www.youtube.com/@mzikmund",
    "CacheTtlMinutes": 30
  }
}
```

**Configuration Class**:

**Location**: `src/web/MZikmund.Web.Configuration/YouTubeOptions.cs`

```csharp
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
    public string FeedUrl { get; set; } = "https://www.youtube.com/feeds/videos.xml?channel_id=UCB6Td35bzTvcJN_HG6TLtwA";

    /// <summary>
    /// YouTube channel URL for "View all videos" / "Subscribe" links.
    /// Format: https://www.youtube.com/@{HANDLE}
    /// </summary>
    public string ChannelUrl { get; set; } = "https://www.youtube.com/@mzikmund";

    /// <summary>
    /// Cache time-to-live in minutes (FR-001: 30 minutes minimum).
    /// </summary>
    public int CacheTtlMinutes { get; set; } = 30;
}
```

**Registration**: `Program.cs`

```csharp
// After other configuration setup
var youtubeConfig = configuration.GetSection("YouTube");
services.Configure<YouTubeOptions>(youtubeConfig);

// Validate required values at startup
var youtubeOptions = youtubeConfig.Get<YouTubeOptions>();
if (string.IsNullOrEmpty(youtubeOptions?.FeedUrl) ||
    !youtubeOptions.FeedUrl.StartsWith("https://www.youtube.com/feeds/videos.xml"))
{
    throw new InvalidOperationException(
        "YouTube:FeedUrl not configured. Set in appsettings.json or environment variable: " +
        "YouTube__FeedUrl=https://www.youtube.com/feeds/videos.xml?channel_id=YOUR_CHANNEL_ID");
}
```

---

## No Database Entities

**Decision**: Videos are read-only external data; no EF Core entities needed.

**Rationale**:
- Videos fetched from YouTube RSS feed in real-time
- Cached in-memory for 30 minutes; no persistence needed
- If later need to store video metadata (watch count, likes, etc.), add `VideoEntity` then
- Principle V (Simplicity & Pragmatism): YAGNI — don't create DB entities for hypothetical future needs

---

## Data Flow

```
YouTube RSS Feed
    ↓
[HTTP Request to FeedUrl]
    ↓
YouTubeRssFeedParser
    (Parse XML → VideoDto list)
    ↓
GetVideosQuery Handler
    (Check cache; if miss, parse; store in cache for 30 min)
    ↓
ICache (In-Memory)
    (Store List<VideoDto>)
    ↓
Controller / Razor Page
    (Render video cards from cached DTOs)
    ↓
Client Browser
    (Display video cards + links to YouTube)
```

---

## State Transitions

Videos have no state transitions. They are immutable snapshots from YouTube RSS feed.

- **Fetched**: Video loaded from RSS feed
- **Cached**: Video stored in-memory cache
- **Stale**: Cache expires after 30 minutes → triggers refresh
- **Displayed**: Video rendered to client
- **Linked**: Client follows link to YouTube

---

## Validation Rules

| Rule | Applies To | Action | Error Handling |
|------|-----------|--------|----------------|
| URL is HTTPS | `ThumbnailUrl`, `YouTubeUrl` | Validate scheme | Skip malformed entry; log warning |
| Title non-empty | `Title` | Check length | Skip if empty after trim |
| Description non-empty after trim | `Description` | Check length; trim to 3-4 lines | Skip if empty after trim |
| PublishedDate is valid RFC 3339 | `PublishedDate` | Parse with `DateTimeOffset.Parse()` | Skip if parse fails; log error |
| VideoId is 11 alphanumeric | `VideoId` | Extract from URL; validate format | Skip if extraction fails |
| URL contains valid video ID | `YouTubeUrl` | Parse query string; extract `v=` param | Skip if parse fails |

**Validation Strategy**: **Skip malformed entries; display complete entries only** (per edge case in spec)

**Example**:
```csharp
var validVideos = new List<VideoDto>();

foreach (var item in feed.Items)
{
    try
    {
        var video = ParseVideoFromFeedItem(item);
        if (video != null)  // null if validation fails
            validVideos.Add(video);
    }
    catch (Exception ex)
    {
        _logger.LogWarning($"Skipping malformed video entry: {ex.Message}");
    }
}

return validVideos;
```

---

## API Contracts

See `/contracts/videos-api.md` for endpoint specifications.

---

## Testing Data

Mock YouTube RSS entries for unit tests:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<feed xmlns="http://www.w3.org/2005/Atom" xmlns:yt="http://www.youtube.com/xml/schemas/2015">
  <entry>
    <id>yt:video:dQw4w9WgXcQ</id>
    <yt:videoId>dQw4w9WgXcQ</yt:videoId>
    <title>Building APIs with .NET 10</title>
    <link href="https://www.youtube.com/watch?v=dQw4w9WgXcQ"/>
    <author>
      <name>mzikmund</name>
      <uri>https://www.youtube.com/channel/UCB6Td35bzTvcJN_HG6TLtwA</uri>
    </author>
    <published>2025-02-10T14:30:00Z</published>
    <updated>2025-02-10T14:30:00Z</updated>
    <media:group xmlns:media="http://search.yahoo.com/mrss/">
      <media:title>Building APIs with .NET 10</media:title>
      <media:content url="https://www.youtube.com/v/dQw4w9WgXcQ?fs=1" type="application/x-shockwave-flash"/>
      <media:thumbnail url="https://i.ytimg.com/vi/dQw4w9WgXcQ/maxresdefault.jpg" width="1280" height="720"/>
      <media:description>In this video, we explore the latest features &amp; best practices for building RESTful APIs with .NET 10.</media:description>
    </media:group>
  </entry>
</feed>
```

**Expected VideoDto Output**:
```csharp
new VideoDto
{
    VideoId = "dQw4w9WgXcQ",
    Title = "Building APIs with .NET 10",
    Description = "In this video, we explore the latest features & best practices for building RESTful APIs with .NET 10.",
    ThumbnailUrl = "https://i.ytimg.com/vi/dQw4w9WgXcQ/maxresdefault.jpg",
    PublishedDate = DateTimeOffset.Parse("2025-02-10T14:30:00Z"),
    YouTubeUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
    ChannelId = "UCB6Td35bzTvcJN_HG6TLtwA"
}
```

