# Quick Start: YouTube Videos Integration

**Feature**: 001-youtube-videos | **Date**: February 13, 2025

This guide walks you through implementing the YouTube Videos feature step-by-step. Estimated time: 2-2.5 hours.

---

## Prerequisites

- Visual Studio or VS Code with C# extensions
- .NET 10 SDK (already installed per CLAUDE.md)
- Access to solution: `MZikmund.slnx`
- Verify `Directory.Packages.props` has `System.ServiceModel.Syndication` v10.0.0 (it should)

---

## Implementation Phases

### Phase 1: Create Data Model & Configuration (30 min)

#### 1.1 Create VideoDto

**File**: `src/shared/MZikmund.DataContracts/Videos/VideoDto.cs`

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
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Video description from RSS feed (HTML entities decoded, trimmed to 3-4 lines).
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// URL to YouTube video thumbnail (16:9 aspect ratio).
    /// </summary>
    public required string ThumbnailUrl { get; init; }

    /// <summary>
    /// Publication date/time from YouTube RSS feed.
    /// </summary>
    public required DateTimeOffset PublishedDate { get; init; }

    /// <summary>
    /// Direct YouTube video URL.
    /// </summary>
    public required string YouTubeUrl { get; init; }

    /// <summary>
    /// YouTube channel ID (for reference; from feed metadata).
    /// </summary>
    public string? ChannelId { get; init; }
}
```

#### 1.2 Create YouTubeOptions Configuration Class

**File**: `src/web/MZikmund.Web.Configuration/YouTubeOptions.cs`

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
    /// </summary>
    public string FeedUrl { get; set; } = "https://www.youtube.com/feeds/videos.xml?channel_id=UCB6Td35bzTvcJN_HG6TLtwA";

    /// <summary>
    /// YouTube channel URL for "View all videos" / "Subscribe" links.
    /// </summary>
    public string ChannelUrl { get; set; } = "https://www.youtube.com/@mzikmund";

    /// <summary>
    /// Cache time-to-live in minutes (minimum 30 per requirements).
    /// </summary>
    public int CacheTtlMinutes { get; set; } = 30;
}
```

#### 1.3 Add Configuration to appsettings.json

**File**: `src/web/MZikmund.Web/appsettings.json`

```json
{
  "YouTube": {
    "FeedUrl": "https://www.youtube.com/feeds/videos.xml?channel_id=UCB6Td35bzTvcJN_HG6TLtwA",
    "ChannelUrl": "https://www.youtube.com/@mzikmund",
    "CacheTtlMinutes": 30
  }
}
```

#### 1.4 Register Configuration in Program.cs

**File**: `src/web/MZikmund.Web/Program.cs` (in the services configuration section)

Find the section where other configurations are registered (around line 50) and add:

```csharp
// YouTube RSS feed configuration
var youtubeConfig = builder.Configuration.GetSection("YouTube");
builder.Services.Configure<YouTubeOptions>(youtubeConfig);

// Validate YouTube configuration at startup
var youtubeOptions = youtubeConfig.Get<YouTubeOptions>();
if (string.IsNullOrEmpty(youtubeOptions?.FeedUrl) ||
    !youtubeOptions.FeedUrl.StartsWith("https://www.youtube.com/feeds/videos.xml"))
{
    throw new InvalidOperationException(
        "YouTube:FeedUrl not configured correctly. Set in appsettings.json: " +
        "YouTube:FeedUrl=https://www.youtube.com/feeds/videos.xml?channel_id=YOUR_CHANNEL_ID");
}
```

---

### Phase 2: Create Service Layer (45 min)

#### 2.1 Enhance ICache Interface

**File**: `src/web/MZikmund.Web.Core/Services/ICache.cs`

```csharp
namespace MZikmund.Web.Core.Services;

public interface ICache
{
    /// <summary>
    /// Gets a cached value by key.
    /// </summary>
    /// <typeparam name="T">The type of value to retrieve.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The cached value or null if not found or expired.</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);

    /// <summary>
    /// Sets a value in the cache with expiration.
    /// </summary>
    /// <typeparam name="T">The type of value to cache.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="ttl">Time-to-live duration.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default);

    /// <summary>
    /// Removes a value from the cache.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RemoveAsync(string key, CancellationToken ct = default);
}
```

#### 2.2 Implement ICache with IMemoryCache

**File**: `src/web/MZikmund.Web.Core/Services/Cache.cs`

```csharp
using Microsoft.Extensions.Caching.Memory;

namespace MZikmund.Web.Core.Services;

public class Cache : ICache
{
    private readonly IMemoryCache _memoryCache;

    public Cache(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        return await Task.FromResult(_memoryCache.TryGetValue(key, out T? value) ? value : default);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default)
    {
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        };

        _memoryCache.Set(key, value, cacheOptions);
        await Task.CompletedTask;
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        _memoryCache.Remove(key);
        await Task.CompletedTask;
    }
}
```

#### 2.3 Create RSS Feed Parser Service

**File**: `src/web/MZikmund.Web.Core/Features/Videos/RssParsing/YouTubeRssFeedParser.cs`

```csharp
using System.Net.Http;
using System.ServiceModel.Syndication;
using System.Xml;
using MZikmund.DataContracts.Videos;
using Microsoft.Extensions.Logging;

namespace MZikmund.Web.Core.Features.Videos.RssParsing;

/// <summary>
/// Parses YouTube RSS feed and converts to VideoDto objects.
/// </summary>
public class YouTubeRssFeedParser
{
    private readonly ILogger<YouTubeRssFeedParser> _logger;
    private readonly HttpClient _httpClient;

    public YouTubeRssFeedParser(ILogger<YouTubeRssFeedParser> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
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

            using (var httpStream = await _httpClient.GetStreamAsync(feedUrl, ct))
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
                        _logger.LogWarning($"Skipping malformed video entry: {ex.Message}");
                    }
                }

                // Sort by date descending (newest first)
                videos = videos
                    .OrderByDescending(v => v.PublishedDate)
                    .ToList();

                _logger.LogInformation($"Successfully parsed {videos.Count} videos from YouTube RSS feed");
                return videos;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError($"Failed to fetch YouTube RSS feed: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error parsing YouTube RSS feed: {ex.Message}");
            throw;
        }
    }

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

        // Extract thumbnail from media content
        var thumbnailUrl = item.ElementExtensions
            .FirstOrDefault(e => e.OuterName == "thumbnail")
            ?.GetReader()
            ?.GetAttribute("url") ?? "https://via.placeholder.com/640x360?text=Video";

        // Extract description
        var description = item.Summary?.Text ?? item.Content?.Text ?? "";
        description = System.Net.WebUtility.HtmlDecode(description);
        description = TrimDescription(description, 3);  // Trim to 3-4 lines

        // Validate thumbnail URL
        if (!Uri.TryCreate(thumbnailUrl, UriKind.Absolute, out var thumbnailUri) ||
            thumbnailUri.Scheme != "https")
        {
            thumbnailUrl = "https://via.placeholder.com/640x360?text=Video";
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
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            return query["v"];
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

        var trimmed = text.Substring(0, maxChars);
        var lastSpace = trimmed.LastIndexOf(' ');

        return lastSpace > 0 ? trimmed.Substring(0, lastSpace) + "…" : trimmed + "…";
    }
}
```

#### 2.4 Register Parser and HttpClient in Program.cs

**File**: `src/web/MZikmund.Web/Program.cs`

Add these lines in the services configuration section (around where ICache is registered):

```csharp
// YouTube RSS feed parser
builder.Services.AddHttpClient<YouTubeRssFeedParser>();
builder.Services.AddScoped<YouTubeRssFeedParser>();
```

Don't forget to add the using statement at the top:
```csharp
using MZikmund.Web.Core.Features.Videos.RssParsing;
```

---

### Phase 3: Create Query Handler (30 min)

#### 3.1 Create GetVideosQuery

**File**: `src/web/MZikmund.Web.Core/Features/Videos/Queries/GetVideosQuery.cs`

```csharp
using MediatR;
using MZikmund.DataContracts.Videos;

namespace MZikmund.Web.Core.Features.Videos.Queries;

/// <summary>
/// Query to fetch videos from YouTube RSS feed (with caching).
/// </summary>
public class GetVideosQuery : IRequest<List<VideoDto>?>
{
    /// <summary>
    /// Optional: limit to the N most recent videos.
    /// If null, returns all available videos.
    /// </summary>
    public int? Count { get; set; }
}
```

#### 3.2 Create GetVideosQueryHandler

**File**: `src/web/MZikmund.Web.Core/Features/Videos/Queries/GetVideosQueryHandler.cs`

```csharp
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
```

---

### Phase 4: Create API Controller (20 min)

#### 4.1 Create VideosController

**File**: `src/web/MZikmund.Web/Controllers/VideosController.cs`

```csharp
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MZikmund.Web.Core.Features.Videos.Queries;

namespace MZikmund.Web.Controllers;

/// <summary>
/// API endpoints for YouTube videos.
/// </summary>
[ApiController]
[Route("api/[controller]")]
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

        return Ok(new { data = videos });
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

        return Ok(new { data = videos });
    }
}
```

---

### Phase 5: Create Razor Pages (45 min)

#### 5.1 Create Videos Page

**File**: `src/web/MZikmund.Web/Pages/Videos.cshtml`

```html
@page
@model VideoModel
@{
    ViewData["Title"] = "Videos";
}

<div class="container py-5 videos-page">
    <div class="row mb-5">
        <div class="col-md-8">
            <h1 class="display-4">@ViewData["Title"]</h1>
            <p class="lead text-muted">Latest videos from my YouTube channel covering .NET, C#, and web development.</p>
        </div>
        <div class="col-md-4 text-end">
            <a href="@Model.YouTubeChannelUrl" target="_blank" rel="noopener" class="btn btn-danger btn-lg">
                <i class="bi bi-youtube"></i> Subscribe on YouTube
            </a>
        </div>
    </div>

    @if (Model.Videos?.Count > 0)
    {
        <div class="row">
            @foreach (var video in Model.Videos)
            {
                <partial name="Shared/_VideoListItem" model="video" />
            }
        </div>
    }
    else
    {
        <div class="alert alert-info" role="alert">
            <h4 class="alert-heading">No videos available yet</h4>
            <p>Check back soon for new content! In the meantime, you can:</p>
            <a href="@Model.YouTubeChannelUrl" target="_blank" rel="noopener" class="btn btn-outline-danger">
                Visit my YouTube channel
            </a>
        </div>
    }

    <footer class="mt-5 pt-4 border-top text-center text-muted">
        <p>Videos powered by <a href="https://www.youtube.com" target="_blank" rel="noopener">YouTube</a></p>
        <img src="~/images/youtube-logo.svg" alt="YouTube Logo" class="youtube-logo-footer">
    </footer>
</div>
```

#### 5.2 Create Videos.cshtml.cs Page Model

**File**: `src/web/MZikmund.Web/Pages/Videos.cshtml.cs`

```csharp
using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using MZikmund.DataContracts.Videos;
using MZikmund.Web.Configuration;
using MZikmund.Web.Core.Features.Videos.Queries;

namespace MZikmund.Web.Pages;

public class VideoModel : PageModel
{
    private readonly IMediator _mediator;
    private readonly IOptions<YouTubeOptions> _options;

    public List<VideoDto>? Videos { get; set; }
    public string YouTubeChannelUrl { get; set; } = "";

    public VideoModel(IMediator mediator, IOptions<YouTubeOptions> options)
    {
        _mediator = mediator;
        _options = options;
    }

    public async Task OnGetAsync()
    {
        YouTubeChannelUrl = _options.Value.ChannelUrl;
        Videos = await _mediator.Send(new GetVideosQuery());
    }
}
```

#### 5.3 Create Video List Item Partial

**File**: `src/web/MZikmund.Web/Pages/Shared/_VideoListItem.cshtml`

```html
@model MZikmund.DataContracts.Videos.VideoDto

<div class="col-md-6 col-lg-4 mb-4 d-flex align-items-stretch">
    <article class="article-card card border-0 shadow-sm h-100 w-100">
        <a href="@Model.YouTubeUrl" target="_blank" rel="noopener"
           class="card-link-wrapper" aria-label="Watch video on YouTube: @Model.Title">

            <!-- VIDEO THUMBNAIL -->
            <div class="article-card-image-wrapper">
                <img class="card-img-top article-card-image"
                     src="@Model.ThumbnailUrl"
                     alt="@Model.Title"
                     loading="lazy">
                <div class="video-card-overlay">
                    <span class="bi bi-play-circle"></span>
                </div>
            </div>

            <!-- CONTENT -->
            <div class="card-body d-flex flex-column">

                <!-- DATE -->
                <div class="article-meta mb-2">
                    <small class="text-muted">
                        <i class="bi bi-calendar3"></i>
                        @Model.PublishedDate.ToString("MMM d, yyyy")
                    </small>
                </div>

                <!-- TITLE -->
                <h3 class="article-title h5 mb-3">@Model.Title</h3>

                <!-- DESCRIPTION -->
                <div class="article-abstract text-muted mb-3 flex-grow-1">
                    @Model.Description
                </div>

                <!-- CTA BUTTON -->
                <div class="article-read-more mt-auto">
                    <span class="btn btn-sm btn-outline-danger">
                        <i class="bi bi-play-fill"></i> Watch on YouTube →
                    </span>
                </div>
            </div>
        </a>
    </article>
</div>
```

#### 5.4 Add Videos Tab to Navigation

**File**: `src/web/MZikmund.Web/Pages/Shared/_Layout.cshtml`

Find the navigation menu section (usually `<nav>` element) and add:

```html
<li class="nav-item">
    <a class="nav-link" asp-page="/Videos">@Localizer["Videos"]</a>
</li>
```

---

### Phase 6: Add Featured Videos to Home Page (20 min)

#### 6.1 Add CSS Styles to SCSS

**File**: `src/web/MZikmund.Web/wwwroot/scss/site.scss`

Add at the end of the file:

```scss
// Video card styles
.video-card-overlay {
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  font-size: 3rem;
  color: rgba(255, 255, 255, 0.8);
  opacity: 0;
  transition: opacity 0.3s ease;
}

.article-card:hover .video-card-overlay {
  opacity: 1;
}

.btn-outline-danger:hover {
  background-color: #dc3545;
  color: white;
}

.featured-videos {
  background-color: var(--bs-gray-100);

  h2 {
    font-weight: 700;
    margin-bottom: 1rem;
  }

  .lead {
    font-size: 1.1rem;
  }
}

.videos-page {
  .display-4 {
    font-weight: 700;
    margin-bottom: 1rem;
  }

  .lead {
    font-size: 1.1rem;
  }
}
```

#### 6.3 Update Home Page Model

**File**: `src/web/MZikmund.Web/Pages/Index.cshtml.cs`

Add to the OnGet() method:

```csharp
// Fetch latest 3 videos for featured section
FeaturedVideos = await _mediator.Send(new GetVideosQuery { Count = 3 });
```

Add property:
```csharp
public List<VideoDto>? FeaturedVideos { get; set; }
```

Don't forget the using statement:
```csharp
using MZikmund.Web.Core.Features.Videos.Queries;
```

#### 6.4 Add Featured Videos Section to Home Page

**File**: `src/web/MZikmund.Web/Pages/Index.cshtml`

Add this section after blog posts section:

```html
@if (Model.FeaturedVideos?.Count > 0)
{
    <section class="featured-videos py-5">
        <div class="container">
            <div class="row mb-4">
                <div class="col-md-8">
                    <h2 class="display-5">Latest Videos</h2>
                    <p class="lead text-muted">Check out my latest video content on YouTube</p>
                </div>
                <div class="col-md-4 text-end">
                    <a asp-page="/Videos" class="btn btn-outline-primary btn-lg">
                        View all videos →
                    </a>
                </div>
            </div>
            <div class="row">
                @foreach (var video in Model.FeaturedVideos)
                {
                    <partial name="Shared/_VideoListItem" model="video" />
                }
            </div>
        </div>
    </section>
}
```

---

### Phase 7: Testing (30 min)

#### 8.1 Unit Tests

**File**: `src/web/MZikmund.Web.Core.Tests/Features/Videos/YouTubeRssFeedParserTests.cs`

```csharp
using Xunit;
using MZikmund.Web.Core.Features.Videos.RssParsing;
using Microsoft.Extensions.Logging;
using Moq;

namespace MZikmund.Web.Core.Tests.Features.Videos;

public class YouTubeRssFeedParserTests
{
    private readonly YouTubeRssFeedParser _parser;
    private readonly Mock<ILogger<YouTubeRssFeedParser>> _loggerMock;

    public YouTubeRssFeedParserTests()
    {
        _loggerMock = new Mock<ILogger<YouTubeRssFeedParser>>();
        var httpClientMock = new HttpClient();
        _parser = new YouTubeRssFeedParser(_loggerMock.Object, httpClientMock);
    }

    [Fact]
    public async Task ParseAsync_WithValidFeed_ReturnsOrderedVideos()
    {
        // Arrange
        var feedUrl = "https://www.youtube.com/feeds/videos.xml?channel_id=CHANNEL_ID";

        // Act
        var videos = await _parser.ParseAsync(feedUrl);

        // Assert
        Assert.NotNull(videos);
        Assert.NotEmpty(videos);
        // Verify reverse chronological order
        for (int i = 1; i < videos.Count; i++)
        {
            Assert.True(videos[i - 1].PublishedDate >= videos[i].PublishedDate);
        }
    }

    [Fact]
    public void ParseFeedItem_WithValidItem_ExtractsCorrectData()
    {
        // Test parsing logic here
    }
}
```

#### 8.2 Integration Tests

Test endpoints via API:

```bash
# Test get all videos
curl https://localhost:5001/api/videos

# Test get latest 3 videos
curl https://localhost:5001/api/videos/latest?count=3

# Test invalid count
curl https://localhost:5001/api/videos/latest?count=999
```

#### 8.3 Manual Testing

1. **Run the application**:
   ```bash
   cd src/web/MZikmund.Web
   dotnet run
   ```

2. **Test Videos tab**: Navigate to `https://localhost:5001/Videos`

3. **Test home page featured section**: Navigate to `https://localhost:5001/`

4. **Test API endpoints**: Use browser or Postman to call `/api/videos`

5. **Test caching**: Call API twice within 30 seconds; note timestamps (second should be instant)

6. **Test error handling**: Temporarily change FeedUrl to invalid URL in appsettings.json; verify 503 error

---

## Verification Checklist

- [ ] `VideoDto` created in `MZikmund.DataContracts.Videos`
- [ ] `YouTubeOptions` configuration class created
- [ ] Configuration added to `appsettings.json`
- [ ] Configuration registered in `Program.cs` with validation
- [ ] `ICache` interface enhanced with Get/Set/Remove methods
- [ ] `Cache` implementation updated
- [ ] `YouTubeRssFeedParser` service created
- [ ] Parser registered in `Program.cs`
- [ ] `GetVideosQuery` and handler created
- [ ] `VideosController` API endpoints implemented
- [ ] `/Videos` page created with model
- [ ] `_VideoListItem` partial created
- [ ] CSS styles added to `site.scss` (video overlay, button hover, featured section)
- [ ] Videos tab added to navigation
- [ ] Featured videos section added to home page
- [ ] Unit tests created and passing
- [ ] Integration tests verify API endpoints
- [ ] Manual testing: Videos tab displays videos
- [ ] Manual testing: Home page shows featured videos
- [ ] Manual testing: API endpoints return correct JSON
- [ ] Manual testing: Cache works (2nd request is fast)
- [ ] Manual testing: Error handling (503 on feed failure)

---

## Common Issues & Fixes

### Issue: HttpRequestException when fetching feed

**Cause**: Feed URL unreachable or network error

**Fix**: Verify YouTube channel ID is correct in `appsettings.json`. Check network connectivity.

### Issue: Videos not displaying in reverse chronological order

**Cause**: Parsing is not sorting correctly

**Fix**: Verify `ParseAsync` includes `.OrderByDescending(v => v.PublishedDate)`.

### Issue: Cache not working; videos fetched every time

**Cause**: `ICache.SetAsync` not being called or `GetAsync` returning null

**Fix**: Verify `IMemoryCache` is registered in `Program.cs`. Check cache key matches between Get/Set calls.

### Issue: CORS errors when calling API from browser

**Cause**: CORS policy not configured for API calls

**Fix**: Verify `"WasmAppPolicy"` CORS policy includes `/api/*` routes, or create a new public API CORS policy.

---

## Next Steps

1. **Code review**: Get feedback on implementation
2. **Staging deployment**: Deploy to staging environment; verify against real YouTube feed
3. **Production deployment**: Deploy to production (via Azure CI/CD in `.github/workflows/`)
4. **Monitoring**: Monitor cache hit rates, feed fetch errors, API latency in Application Insights
5. **Future enhancements** (if needed):
   - Add video search/filtering
   - Add watch time tracking
   - Add comment integration with YouTube API
   - Store video metadata in database for analytics

