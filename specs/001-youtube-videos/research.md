# Research Findings: YouTube Videos Integration

**Date**: February 13, 2025 | **Feature**: 001-youtube-videos

## Summary

All unknowns resolved. Feature is ready for Phase 1 design. Key decisions made based on codebase investigation:

1. ✅ **RSS Library**: Use `System.ServiceModel.Syndication` (already installed)
2. ✅ **Caching**: Enhance empty `ICache` interface; follow singleton DI pattern
3. ✅ **UI Component**: Reuse existing blog card partial with parameterization
4. ✅ **Error Handling**: Follow null-coalescing pattern (return null, not exceptions)
5. ✅ **HTML Safety**: Markdig escapes HTML; safe to render descriptions

---

## Research Results

### Unknown 1: RSS Parsing Library ✅ RESOLVED

**Decision**: Use `System.ServiceModel.Syndication` v10.0.0 (already installed in `Directory.Packages.props`)

**Rationale**:
- Zero new dependencies needed
- Already used in project (installed, managed centrally)
- Type-safe SyndicationFeed API
- Consistent with existing `IFeedGenerator` pattern for RSS handling
- 40-50 lines of code for handler implementation
- Well-documented for YouTube RSS format (standard RSS 2.0 with media namespace)

**Alternatives Considered**:
- **XDocument (System.Xml.Linq)**: Built-in, minimal, but lower-level XML manipulation
  - Pros: Zero dependencies, familiar to .NET developers
  - Cons: More parsing code, manual namespace handling
  - Rejected: SyndicationFeed provides higher-level abstraction for cleaner code

- **Edi.SyndicationFeed**: Streaming support, lightweight
  - Pros: Efficient for large feeds (1000+ items)
  - Cons: Less type-safe, additional dependency
  - Rejected: YouTube channels typically 15-20 recent videos; streaming not needed

**Implementation Pattern**:
```csharp
var reader = XmlReader.Create("https://www.youtube.com/feeds/videos.xml?channel_id=...");
var feed = SyndicationFeed.Load(reader);
var entries = feed.Items
    .Select(item => new VideoDto { /* map properties */ })
    .OrderByDescending(v => v.PublishedDate)
    .ToList();
```

**YouTube RSS Structure**: Standard RSS 2.0 with media namespace:
- `<title>` → video title
- `<description>` → video description (HTML entities, ~2-3 sentences)
- `<media:thumbnail url="...">` → thumbnail URL
- `<published>` → publication date (RFC 3339)
- `<link>` → YouTube video URL (`https://www.youtube.com/watch?v=...`)

**Special Handling**:
- Author field: YouTube includes as "yt:channelId" (extract if needed)
- Date parsing: RFC 3339 format; use `DateTimeOffset.Parse()`
- Description trimming: Already have `IPostContentProcessor` pattern for markdown; YouTube descriptions are plain text with HTML entities

---

### Unknown 2: Cache Service Integration ✅ RESOLVED

**Decision**: Enhance `ICache` interface with standard cache methods; integrate via singleton DI pattern already established

**Codebase Facts**:
- **Interface**: Empty (`D:\Personal\mzikmund\src\web\MZikmund.Web.Core\Services\ICache.cs`)
- **Implementation**: `Cache` class wraps `IMemoryCache` from ASP.NET Core
- **Registration**: Singleton in `Program.cs` (line 32): `services.AddSingleton<ICache, Cache>()`
- **Current Usage**: **NOT used anywhere yet** — interface is scaffolded, waiting for features like this

**Recommended Interface Enhancement**:
```csharp
public interface ICache
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
}
```

**Cache Key Convention** (follow existing patterns):
- `cache:videos:all` — All videos list
- `cache:videos:latest:3` — Latest 3 videos (home page)
- `cache:videos:timestamp` — Last fetch timestamp for TTL validation

**Caching Pattern** (established in codebase via `WeatherCache` in Uno app):
```csharp
// Handler pseudo-code
public async Task<List<VideoDto>> Handle(GetVideosQuery request, CancellationToken ct)
{
    var cacheKey = "cache:videos:all";

    // Try cache first
    var cached = await _cache.GetAsync<List<VideoDto>>(cacheKey, ct);
    if (cached is not null)
        return cached;

    // Fetch fresh data
    var feed = await _youtubeRssFeedParser.ParseAsync(ct);

    // Cache for 30 minutes
    await _cache.SetAsync(cacheKey, feed, TimeSpan.FromMinutes(30), ct);

    return feed;
}
```

**TTL Implementation**: 30-minute cache per FR-009. Use `CacheItemPolicy` or simple timestamp comparison for absolute expiration.

---

### Unknown 3: Video Card Component Reuse ✅ RESOLVED

**Decision**: Reuse existing `_PostListItem.cshtml` partial with parameter changes; no new card CSS needed

**Codebase Facts**:
- **Component Location**: `D:\Personal\mzikmund\src\web\MZikmund.Web\Pages\Shared\_PostListItem.cshtml`
- **Data Model**: `PostListItem` DTO in `MZikmund.DataContracts`
- **Styling**: CSS classes already defined in `site.css` (`.article-card`, `.article-card-image`, etc.)
- **No Hardcoded Blog References**: HTML structure is fully generic

**Reusability Assessment**:
| Property | Blog Card | Video Card | Mapping |
|----------|-----------|-----------|---------|
| `HeroImageUrl` | Post featured image | YouTube thumbnail | 1:1 match |
| `Title` | Post title | Video title | 1:1 match |
| `Abstract` | Trimmed post content | Trimmed video description | 1:1 match |
| `PublishedDate` | Post publication | Video publication | 1:1 match |
| `RouteName` | Post slug (URL) | Not needed | Unused |
| Navigation Link | `/Blog/Post` (hardcoded) | YouTube video URL | **Needs parameterization** |
| CTA Button | "Read more" | "Watch on YouTube" | **Needs parameterization** |
| Aria-label | "Read blog post" | "Watch video" | **Needs parameterization** |

**Implementation Approach**:
1. **Option A (Recommended)**: Create `_VideoListItem.cshtml` that copies the structure but replaces hardcoded values:
   - Navigation target: Direct YouTube URL (`@Model.YouTubeUrl`)
   - CTA text: "Watch on YouTube"
   - Aria-label: "Watch video: @Model.Title"

2. **Option B (Future-proof)**: Create generic `_GenericCardItem.cshtml` with parameterizable link and button text
   - Slightly more effort upfront
   - Better long-term if more card types added later

**Card Styling Validation**:
- ✅ `.article-card` — Generic card container with hover effects (shadow, scale)
- ✅ `.article-card-image-wrapper` — 16:9 aspect ratio, safe for thumbnails
- ✅ `.article-title` — Title styling, works for any content
- ✅ `.article-abstract` — Text clamp to 3 lines (meets FR-007 requirement)
- ✅ `.article-meta` — Date with calendar icon, reusable
- ⚠️ `.article-card-categories` / `.article-card-category` — CSS missing (not defined in `site.css`)
  - Not critical for videos (videos have no categories)
  - Can be added if needed for future features

**Conclusion**: Component is 95% reusable. Only need to create video-specific partial with parameter changes to links and text.

---

### Unknown 4: Error Handling Pattern ✅ RESOLVED

**Decision**: Follow null-coalescing pattern (return `null`, not exceptions) for external feed failures; let middleware handle errors

**Codebase Pattern** (from `SyndicationDataSource`):
```csharp
// Return null for missing data, not exceptions
public async Task<IReadOnlyList<FeedEntry>?> GetFeedDataAsync(...)
{
    var category = await _repository.GetAsync(...);
    if (category is null)
    {
        return null;  // ← Graceful degradation
    }
    // ... fetch data
    return itemCollection;
}

// Handler checks for null
public async Task<string?> Handle(GetRssQuery request, CancellationToken ct)
{
    var data = await _syndicationDataSource.GetFeedDataAsync(...);
    if (data is null)
    {
        return null;  // ← Return null from handler
    }
    return await _feedGenerator.GetRssAsync(data, ...);
}

// Controller converts null to HTTP status
public async Task<IActionResult> GenerateRss(GetRssQuery query)
{
    var xml = await _mediator.Send(query);
    if (string.IsNullOrWhiteSpace(xml))
    {
        return NotFound();  // ← Return 404 to client
    }
    return Content(xml, "application/rss+xml");
}
```

**Implementation for Videos**:
1. **Query Handler** returns `VideoDto[]?` (nullable)
2. **If feed fetch fails** (network error, timeout, malformed XML):
   - Return `null` from handler (not throw exception)
   - Log warning: `_logger.LogWarning("YouTube RSS feed unavailable")`
3. **If cache available and fetch fails**:
   - Return cached data instead of null (per FR-006 requirement)
   - Log: `_logger.LogWarning("Using cached videos due to feed unavailable")`
4. **If no cache and fetch fails**:
   - Return `null`
5. **Controller handles null**:
   ```csharp
   var videos = await _mediator.Send(new GetVideosQuery());
   if (videos == null || videos.Length == 0)
   {
       return Ok(new List<VideoDto>()); // ← Return 200 with empty array
       // or render view with empty state message
   }
   return Ok(videos);
   ```

**Logging Pattern** (from `SyncBlobMetadataHandler`):
```csharp
_logger = logger;  // Injected via DI

public async Task<Result> Handle(Command request, CancellationToken ct)
{
    _logger.LogInformation("Fetching YouTube videos");

    try
    {
        var feed = await _parser.ParseAsync(ct);
        _logger.LogInformation($"Successfully fetched {feed.Count} videos");
        return feed;
    }
    catch (HttpRequestException ex)
    {
        _logger.LogWarning($"YouTube feed fetch failed: {ex.Message}");
        return await _cache.GetAsync("cache:videos:all", ct);  // Fallback to cache
    }
}
```

**Exception Handling**:
- **DO NOT** throw exceptions in handlers for network/external failures
- **DO** throw `InvalidOperationException` for data validation errors (e.g., "Feed URL not configured")
- **DO** let middleware catch and log unexpected exceptions
- **DO** use DI-injected `ILogger<T>` for all logging

**HTTP Status Codes**:
- `200 OK` with empty array: Feed unavailable but handled gracefully
- `200 OK` with videos: Normal response
- `500 Internal Server Error`: Unexpected error (caught by middleware)
- Controller typically returns `Ok()` not `NotFound()` for missing feed (graceful degradation)

---

### Unknown 5: HTML Content Safety ✅ RESOLVED

**Decision**: YouTube descriptions are plain text with HTML entities; safe to render via `@Html.Raw()` after Markdig processing or entity decoding

**Codebase Pattern** (from blog posts):
```csharp
// ProcessAsync in IPostContentProcessor
public async Task<string> ProcessAsync(string postContent)
{
    // Step 1: Replace Gist URLs with embed scripts
    var content = ReplaceGistUrlsWithEmbedScripts(postContent);

    // Step 2: Convert Markdown to HTML via Markdig
    content = _markdownConverter.ToHtml(content);

    return content;  // Returns safe HTML (Markdig escapes raw HTML)
}
```

**Markdig Security**:
- ✅ **Escapes HTML by default**: Raw HTML tags in markdown input are escaped
- ✅ **Prevents XSS**: `<script>alert('xss')</script>` becomes `&lt;script&gt;...&lt;/script&gt;`
- ✅ **Allows safe tags**: HTML-encoded output is safe to render with `@Html.Raw()`

**HTML Entity Handling**:
YouTube RSS descriptions contain:
- `&quot;` → `"`
- `&amp;` → `&`
- `&lt;` → `<`
- `&gt;` → `>`
- `&#39;` → `'`

**Options for Video Descriptions**:

**Option A (Recommended)**: Decode entities then pass through Markdig
```csharp
var decoded = System.Net.WebUtility.HtmlDecode(description);  // YouTube entities → text
var html = _markdownConverter.ToHtml(decoded);  // Markdown → safe HTML
// Then @Html.Raw(html) in Razor
```

**Option B**: Decode entities only
```csharp
var decoded = System.Net.WebUtility.HtmlDecode(description);
// Then @Model.Description in Razor (auto-encoded, safe)
```

**Option C**: Use as-is (entities remain encoded)
```csharp
// @Html.Raw(description) — entities remain as `&quot;` etc., safe
```

**Current Blog Pattern** (from `_PostListItem.cshtml`):
```html
<div class="article-abstract text-muted mb-3 flex-grow-1">
    @Html.Raw(Model.Abstract)  // ← Uses Raw() for processed content
</div>
```

**Recommendation for Videos**:
- Decode YouTube entities using `WebUtility.HtmlDecode()`
- Store decoded text in `VideoDto.Description`
- Render with `@Html.Raw()` in card partial (already safe, no markdown needed)
- This keeps descriptions readable without HTML entity rendering

**Safety Validation**:
- ✅ Hero images: URI validation (HTTP/HTTPS only) — already in blog pattern
- ✅ Descriptions: Entity decoding only; no script tags from YouTube
- ✅ Titles: Plain text; no special handling needed
- ✅ Links: Direct YouTube URLs; validated as absolute HTTPS URIs
- ✅ No Global CSP: Not a blocker (admin-controlled content)

**Conclusion**: YouTube descriptions are inherently safe (read-only from YouTube RSS, no user HTML input). Decode entities and render with `@Html.Raw()` following existing blog pattern.

---

## Design Decisions Summary

| Unknown | Decision | Confidence |
|---------|----------|-----------|
| RSS Library | `System.ServiceModel.Syndication` | 100% (installed, pattern exists) |
| Caching | Enhance `ICache`; use singleton DI | 100% (infrastructure ready) |
| UI Component | Reuse `_PostListItem.cshtml` | 95% (highly generic) |
| Error Handling | Null-coalescing pattern | 100% (established pattern) |
| HTML Safety | Entity decoding + `@Html.Raw()` | 100% (existing pattern) |

**No Blockers. Feature is ready for Phase 1 design.**
