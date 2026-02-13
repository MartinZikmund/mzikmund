# API Contracts: YouTube Videos Integration

**Feature**: 001-youtube-videos | **Date**: February 13, 2025

## Overview

Public API endpoints for retrieving YouTube videos. No authentication required; all endpoints return public YouTube data.

---

## Endpoints

### 1. GET /api/videos

**Description**: Fetch all available videos from the YouTube channel RSS feed

**Requirement Mapping**: FR-001, FR-002, FR-003, FR-004, FR-009

#### Request

```http
GET /api/videos HTTP/1.1
Host: mzikmund.app
Accept: application/json
```

**Query Parameters**: None

**Authentication**: Not required (public endpoint)

#### Response

**Status**: `200 OK`

**Content-Type**: `application/json`

**Body** (Success):
```json
{
  "data": [
    {
      "videoId": "dQw4w9WgXcQ",
      "title": "Building APIs with .NET 10",
      "description": "In this video, we explore the latest features & best practices for building RESTful APIs with .NET 10.",
      "thumbnailUrl": "https://i.ytimg.com/vi/dQw4w9WgXcQ/maxresdefault.jpg",
      "publishedDate": "2025-02-10T14:30:00Z",
      "youtubeUrl": "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
      "channelId": "UCB6Td35bzTvcJN_HG6TLtwA"
    },
    {
      "videoId": "abc123xyz789",
      "title": "C# 13 New Features",
      "description": "Discover what's new in C# 13: records, pattern matching, nullable reference types...",
      "thumbnailUrl": "https://i.ytimg.com/vi/abc123xyz789/maxresdefault.jpg",
      "publishedDate": "2025-02-09T10:00:00Z",
      "youtubeUrl": "https://www.youtube.com/watch?v=abc123xyz789",
      "channelId": "UCB6Td35bzTvcJN_HG6TLtwA"
    }
  ]
}
```

**Notes**:
- Videos returned in **reverse chronological order** (newest first) per FR-003
- Response is cached for 30 minutes (FR-001, FR-009)
- Cache refreshes automatically; subsequent requests within 30 minutes serve cached response

**Status**: `200 OK` (Even if empty)

**Body** (No Videos Available):
```json
{
  "data": []
}
```

**Notes**:
- If YouTube RSS feed returns zero videos, return empty array with 200 OK
- Frontend displays empty state message (FR-010)

**Status**: `503 Service Unavailable` (If feed fetch fails and no cache available)

**Body**:
```json
{
  "error": "YouTube feed temporarily unavailable. Please try again later.",
  "timestamp": "2025-02-13T12:00:00Z"
}
```

**Notes**:
- Only if feed is unreachable AND no cached data available
- Per FR-006: Graceful error handling
- Cache may serve stale data if available (FR-006 edge case)

#### Caching

- **Cache Key**: `cache:videos:all`
- **TTL**: 30 minutes (configurable via `YouTube:CacheTtlMinutes` in `appsettings.json`)
- **Cache Hit Response**: 200 OK (identical payload; cached within last 30 minutes)
- **Cache Miss + Successful Fetch**: 200 OK (fresh data fetched from YouTube RSS feed)
- **Cache Miss + Feed Unavailable**: 503 Service Unavailable with fallback message

#### Example Usage

**cURL**:
```bash
curl -i https://mzikmund.app/api/videos
```

**JavaScript/Fetch**:
```javascript
const response = await fetch('https://mzikmund.app/api/videos');
const { data } = await response.json();
data.forEach(video => {
  console.log(`${video.title} (${video.publishedDate})`);
  console.log(`Watch: ${video.youtubeUrl}`);
});
```

**C# / HttpClient**:
```csharp
var response = await httpClient.GetAsync("/api/videos");
var content = await response.Content.ReadAsStringAsync();
var videos = JsonSerializer.Deserialize<VideoListResponse>(content);
```

---

### 2. GET /api/videos/latest?count=3

**Description**: Fetch the N most recent videos (used for home page featured section)

**Requirement Mapping**: FR-012, FR-013, FR-014

#### Request

```http
GET /api/videos/latest?count=3 HTTP/1.1
Host: mzikmund.app
Accept: application/json
```

**Query Parameters**:
| Name | Type | Required | Default | Constraints |
|------|------|----------|---------|-------------|
| `count` | integer | No | 3 | 1-10 (limit to prevent abuse) |

**Authentication**: Not required (public endpoint)

#### Response

**Status**: `200 OK`

**Content-Type**: `application/json`

**Body**:
```json
{
  "data": [
    {
      "videoId": "dQw4w9WgXcQ",
      "title": "Building APIs with .NET 10",
      "description": "In this video, we explore the latest features & best practices for building RESTful APIs with .NET 10.",
      "thumbnailUrl": "https://i.ytimg.com/vi/dQw4w9WgXcQ/maxresdefault.jpg",
      "publishedDate": "2025-02-10T14:30:00Z",
      "youtubeUrl": "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
      "channelId": "UCB6Td35bzTvcJN_HG6TLtwA"
    },
    {
      "videoId": "abc123xyz789",
      "title": "C# 13 New Features",
      "description": "Discover what's new in C# 13...",
      "thumbnailUrl": "https://i.ytimg.com/vi/abc123xyz789/maxresdefault.jpg",
      "publishedDate": "2025-02-09T10:00:00Z",
      "youtubeUrl": "https://www.youtube.com/watch?v=abc123xyz789",
      "channelId": "UCB6Td35bzTvcJN_HG6TLtwA"
    },
    {
      "videoId": "xyz789abc123",
      "title": "Entity Framework Core Best Practices",
      "description": "Learn EF Core patterns for high-performance applications...",
      "thumbnailUrl": "https://i.ytimg.com/vi/xyz789abc123/maxresdefault.jpg",
      "publishedDate": "2025-02-08T15:00:00Z",
      "youtubeUrl": "https://www.youtube.com/watch?v=xyz789abc123",
      "channelId": "UCB6Td35bzTvcJN_HG6TLtwA"
    }
  ]
}
```

**Notes**:
- Returns at most `count` videos (or fewer if not available)
- Returns same cached data as `/api/videos` but sliced to latest N items
- Can also be fetched from all videos and sliced in-memory (implementation detail)

#### Caching

- **Cache Key**: `cache:videos:latest:{count}`
- **TTL**: 30 minutes (same as main videos endpoint)
- **Strategy**: Could cache separately per count value, or slice from main cache

#### Example Usage

**HTML (home page featured section)**:
```html
<section class="featured-videos">
  <h2>Latest Videos</h2>
  <div id="video-carousel" class="row"></div>
</section>

<script>
  fetch('/api/videos/latest?count=3')
    .then(r => r.json())
    .then(({ data }) => {
      const carousel = document.getElementById('video-carousel');
      data.forEach(video => {
        carousel.innerHTML += `
          <div class="col-md-4">
            <a href="${video.youtubeUrl}" class="card">
              <img src="${video.thumbnailUrl}" alt="${video.title}">
              <h3>${video.title}</h3>
              <p>${video.description.substring(0, 100)}...</p>
              <small>${new Date(video.publishedDate).toLocaleDateString()}</small>
            </a>
          </div>
        `;
      });
    });
</script>
```

---

## Data Models

### VideoDto

**Type**: `MZikmund.DataContracts.Videos.VideoDto`

```csharp
public record VideoDto
{
    public required string VideoId { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string ThumbnailUrl { get; init; }
    public required DateTimeOffset PublishedDate { get; init; }
    public required string YouTubeUrl { get; init; }
    public string? ChannelId { get; init; }
}
```

### VideoListResponse (Wrapper)

**Type**: Implicit in `GET /api/videos` responses

```csharp
public record VideoListResponse
{
    public required List<VideoDto> Data { get; init; }
    public string? Error { get; init; }
    public DateTimeOffset? Timestamp { get; init; }
}
```

---

## Error Handling

### 503 Service Unavailable

**Scenario**: YouTube RSS feed is unreachable (network error, timeout, invalid feed URL)

**Response**:
```json
{
  "data": null,
  "error": "YouTube feed temporarily unavailable. Please try again later.",
  "timestamp": "2025-02-13T12:00:00Z"
}
```

**Status Code**: 503

**Frequency**: Rare; only if feed is unreachable and no cached data available

**Client Handling**:
- Display user-friendly error message on Videos tab
- Render cached data if available (stale cache fallback)
- Show "View all videos on YouTube" link (FR-005)

### 400 Bad Request

**Scenario**: Invalid query parameter (e.g., `count=abc` or `count=999`)

**Response**:
```json
{
  "data": null,
  "error": "Invalid count parameter. Must be integer between 1 and 10.",
  "timestamp": "2025-02-13T12:00:00Z"
}
```

**Status Code**: 400

**Validation**:
- `count` must be integer
- `count` must be between 1 and 10

### 500 Internal Server Error

**Scenario**: Unexpected server error (code exception, malformed configuration, etc.)

**Response**:
```json
{
  "data": null,
  "error": "Internal server error. Request ID: {TraceId}",
  "timestamp": "2025-02-13T12:00:00Z"
}
```

**Status Code**: 500

**Client Handling**:
- Display generic error message
- Log error ID (TraceId) for support
- Offer "Try again" and "Contact support" options

---

## Performance Targets

Per FR-005:

| Scenario | Target | Notes |
|----------|--------|-------|
| Cache hit (95% of requests) | <500ms | Typically <100ms in-memory lookup |
| Cache miss, fresh fetch | <3s | Network latency + XML parsing |
| Cache expiry refresh | <3s | Background refresh, doesn't block user |
| Empty response | 200ms | Minimal processing |

**Measurement**: Server latency only; excludes client network round-trip.

---

## Security & Attribution

**Requirement Mapping**: FR-011, FR-014

### YouTube Attribution

All responses and UI must include YouTube branding per YouTube ToS:

**API Response Header** (optional):
```
Powered-By: YouTube RSS Feed
```

**UI Attribution** (required in Videos page):
```html
<footer>
  <p>Videos provided by YouTube.
     <a href="https://www.youtube.com/@mzikmund">View all videos on YouTube</a>
  </p>
  <img src="/images/youtube-logo.svg" alt="YouTube Logo" style="height: 20px;">
</footer>
```

### No Authentication Required

- Videos are public YouTube data
- No authorization checks needed
- Rate limiting not implemented (trust YouTube's rate limits via RSS feed)

---

## Implementation Notes

### Query Handler (MediatR)

```csharp
public class GetVideosQuery : IRequest<List<VideoDto>>
{
    public int? Count { get; set; }  // null = all videos
}

public class GetVideosQueryHandler : IRequestHandler<GetVideosQuery, List<VideoDto>>
{
    private readonly ILogger<GetVideosQueryHandler> _logger;
    private readonly ICache _cache;
    private readonly YouTubeRssFeedParser _parser;
    private readonly IOptions<YouTubeOptions> _options;

    public async Task<List<VideoDto>> Handle(GetVideosQuery request, CancellationToken ct)
    {
        var cacheKey = request.Count.HasValue
            ? $"cache:videos:latest:{request.Count}"
            : "cache:videos:all";

        // Try cache first
        var cached = await _cache.GetAsync<List<VideoDto>>(cacheKey, ct);
        if (cached is not null)
            return cached;

        // Fetch fresh data
        try
        {
            var feed = await _parser.ParseAsync(_options.Value.FeedUrl, ct);
            var videos = feed
                .OrderByDescending(v => v.PublishedDate)
                .ToList();

            if (request.Count.HasValue)
                videos = videos.Take(request.Count.Value).ToList();

            // Cache for 30 minutes
            await _cache.SetAsync(cacheKey, videos, TimeSpan.FromMinutes(_options.Value.CacheTtlMinutes), ct);

            _logger.LogInformation($"Fetched {videos.Count} videos from YouTube RSS feed");
            return videos;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to fetch YouTube feed: {ex.Message}");
            return null;  // Let controller handle 503 response
        }
    }
}
```

### Controller

```csharp
[ApiController]
[Route("api/[controller]")]
public class VideosController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<VideosController> _logger;

    [HttpGet]
    public async Task<IActionResult> GetVideos([FromQuery] int? count = null)
    {
        if (count.HasValue && (count < 1 || count > 10))
            return BadRequest(new { error = "Invalid count parameter. Must be between 1 and 10." });

        var videos = await _mediator.Send(new GetVideosQuery { Count = count });

        if (videos == null)
            return StatusCode(503, new { error = "YouTube feed temporarily unavailable." });

        return Ok(new { data = videos });
    }
}
```

---

## Testing

### Unit Test Cases

1. **Parse valid YouTube RSS feed** → Returns correct VideoDto list
2. **Parse malformed feed** → Skips malformed entries; returns valid ones
3. **Feed with zero videos** → Returns empty list (200 OK)
4. **Feed unreachable** → Returns null; controller returns 503
5. **Cache hit** → Returns cached data without fetch
6. **Cache miss + fetch success** → Fetches data; caches for 30 min
7. **Cache miss + fetch failure** → Returns cached data if available (stale cache fallback)
8. **Description trimming** → Descriptions max 3-4 lines (~300 chars)
9. **URL validation** → Invalid HTTPS URLs rejected; video skipped
10. **Reverse chronological order** → Videos sorted newest first

### Integration Test Cases

1. **End-to-end: /api/videos** → Returns valid JSON with correct structure
2. **End-to-end: /api/videos/latest?count=3** → Returns exactly 3 videos
3. **Cache behavior** → First request slow (fetch); second request fast (cache)
4. **Error response** → 503 error formatted correctly
5. **YouTube attribution** → Footer includes attribution text and logo

### Mock Data

Use `specs/001-youtube-videos/data-model.md` Testing Data section for mock RSS feed XML.

