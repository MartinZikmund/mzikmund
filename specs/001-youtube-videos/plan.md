# Implementation Plan: YouTube Videos Integration

**Branch**: `001-youtube-videos` | **Date**: February 13, 2025 | **Spec**: `specs/001-youtube-videos/spec.md`
**Input**: Feature specification from `/specs/001-youtube-videos/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Add a Videos tab to the website displaying the latest videos from the YouTube channel via RSS feed. Visitors can click cards to watch videos on YouTube, and the home page shows the three most recent videos. Backend fetches and caches YouTube RSS feed data for 30 minutes; frontend reuses existing card component styling.

## Technical Context

**Language/Version**: C# 13 / .NET 10.0.100 (pinned in `global.json`)
**Primary Dependencies**: HTTP client (System.Net.Http), XDocument (System.Xml.Linq) for RSS parsing, existing `ICache` service for 30-min TTL caching
**Storage**: In-memory cache via `ICache` service; no database persistence needed for feed data
**Testing**: xUnit with Moq (per `MZikmund.Web.Core.Tests`)
**Target Platform**: ASP.NET Core 10 web application; public videos, no authentication required
**Project Type**: Web (ASP.NET Core vertical slice)
**Performance Goals**: Cache hit: <500ms latency (typically <100ms); fresh fetch from YouTube RSS: <3s; 95% of requests served from cache
**Constraints**: 30-minute cache TTL per FR-009; graceful degradation if feed unavailable (FR-006); descriptions trimmed to 3-4 lines (FR-007)
**Scale/Scope**: YouTube RSS feed (typically 15-20 most recent videos); home page shows 3 most recent; Videos tab shows all available

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| **I. Vertical Slice Architecture** | ✅ PASS | Feature will be structured as `Features/Videos/` with queries (GetVideos) and command-like handler for cache refresh |
| **II. Cross-Platform Shared Contracts** | ✅ PASS | Video DTO (`VideoDto`) belongs in `MZikmund.DataContracts` for potential Uno app reuse |
| **III. Security by Default** | ✅ PASS | No authentication required; public YouTube data. No sensitive operations. |
| **IV. Code Quality & Consistency** | ✅ PASS | Will follow `.editorconfig` rules, file-scoped namespaces, tab indentation, naming conventions |
| **V. Simplicity & Pragmatism** | ✅ PASS | Simplest viable: fetch RSS, parse, cache, display via existing card component. No database, no abstractions beyond existing `ICache` |

**Gate Status**: ✅ PASS - No violations. Feature aligns with vertical slices, uses shared contracts, imposes no security risk, follows code quality rules, and starts with minimal complexity.

## Project Structure

### Documentation (this feature)

```text
specs/001-youtube-videos/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code

```text
src/
├── web/
│   ├── MZikmund.Web/
│   │   ├── Controllers/
│   │   │   └── VideosController.cs              # Public endpoint to fetch video list
│   │   └── Pages/
│   │       ├── Videos.cshtml                    # Videos tab page
│   │       └── Videos.cshtml.cs                 # Code-behind
│   │
│   ├── MZikmund.Web.Core/
│   │   ├── Features/Videos/
│   │   │   ├── Queries/
│   │   │   │   ├── GetVideosQuery.cs            # Query handler (MediatR)
│   │   │   │   └── GetVideosQueryHandler.cs
│   │   │   ├── Caching/
│   │   │   │   └── VideosCacheService.cs        # Wrapper around ICache service
│   │   │   └── RssParsing/
│   │   │       └── YouTubeRssFeedParser.cs      # Parse YouTube RSS feed
│   │   └── ...
│   │
│   ├── MZikmund.Web.Core.Tests/
│   │   └── Features/Videos/
│   │       ├── YouTubeRssFeedParserTests.cs
│   │       └── GetVideosQueryHandlerTests.cs
│   │
│   └── MZikmund.Web.Data/  # (no changes needed for this feature)
│
└── shared/
    └── MZikmund.DataContracts/
        ├── Videos/
        │   └── VideoDto.cs                      # Shared DTO (title, description, thumbnail, date, URL, channel)
        └── ...
```

**Structure Decision**: ASP.NET Core web application. Feature follows vertical slice pattern with `Features/Videos/` containing queries and caching logic. Video DTO lives in `MZikmund.DataContracts` for potential future Uno app consumption. Videos controller provides public API endpoint; Razor page uses controller to fetch and display. RSS parsing isolated in dedicated service class.

## Complexity Tracking

No Constitution violations. Feature fully compliant with all principles.

---

# Phase 0: Research & Clarifications

## Unknowns & Research Tasks

### Unknown 1: RSS Parsing Library
**Question**: Which library should parse YouTube RSS feed XML? System.Xml.Linq (built-in) or third-party like SyndicationFeed?
**Impact**: Affects dependency list and parsing implementation complexity.
**Research needed**: Compare built-in XDocument vs. SyndicationFeed for YouTube RSS specifics.

### Unknown 2: Cache Service Integration
**Question**: How to integrate with existing `ICache` service? What's the cache key naming convention?
**Impact**: Determines how cached video data flows through handlers.
**Research needed**: Examine existing `ICache` service implementation and usage patterns in codebase.

### Unknown 3: HTML Sanitization
**Question**: Should descriptions from YouTube RSS be HTML-escaped/sanitized before display in Razor?
**Impact**: Security and content rendering correctness.
**Research needed**: Verify how existing blog cards handle user/external content HTML.

### Unknown 4: Video Card Component Reuse
**Question**: Can we directly reuse the existing blog card component for video cards?
**Impact**: UI code reuse vs. custom component.
**Research needed**: Inspect existing blog card component structure and styling.

### Unknown 5: Error Handling Pattern
**Question**: How should transient RSS feed errors be handled? Retry logic? Fallback to cached data?
**Impact**: User experience during feed outages.
**Research needed**: Review error handling patterns in existing Features for external API failures.

---

# Phase 1: Design & Contracts ✅ COMPLETE

## Artifacts Generated

### 1. Data Model (`data-model.md`) ✅

**Entity**: `VideoDto` (read-only, immutable, no database persistence)
- Fields: VideoId, Title, Description, ThumbnailUrl, PublishedDate, YouTubeUrl, ChannelId
- Source: YouTube RSS feed (external, real-time)
- Caching: In-memory via `ICache` for 30 minutes (no DB needed)
- Validation: Skip malformed entries; display complete entries only
- Configuration: `YouTubeOptions` with FeedUrl, ChannelUrl, CacheTtlMinutes

**No database entities**: YAGNI principle applied; external data only.

### 2. API Contracts (`contracts/videos-api.md`) ✅

**Endpoint 1**: `GET /api/videos` → Returns all videos (cached 30 min)
- Response: `{ data: [VideoDto, ...] }`
- Caching: 30-minute TTL; 95% cache hit rate
- Error: 503 if feed unavailable and no cache
- Requirement mapping: FR-001, FR-002, FR-003, FR-004, FR-009

**Endpoint 2**: `GET /api/videos/latest?count=3` → Returns N most recent videos
- Response: `{ data: [VideoDto, ...] }` (at most N items)
- Caching: Separate cache key per count value
- Error: 400 if count invalid; 503 if feed unavailable
- Requirement mapping: FR-012, FR-013, FR-014

**Data Models**:
- `VideoDto`: Complete video object
- `VideoListResponse`: Wrapper with error handling

**Error Handling**:
- 503 Service Unavailable: Feed unreachable (graceful degradation per FR-006)
- 400 Bad Request: Invalid parameters
- 500 Internal Server Error: Unexpected exceptions (middleware handles)
- 200 OK with empty array: No videos available (per FR-010)

### 3. Quick Start Guide (`quickstart.md`) ✅

7-phase implementation guide with step-by-step code:
- Phase 1: Create data model & configuration (30 min)
- Phase 2: Create service layer (45 min)
- Phase 3: Create query handler (30 min)
- Phase 4: Create API controller (20 min)
- Phase 5: Create Razor pages (45 min)
- Phase 6: Add featured videos to home page & SCSS styles (25 min)
- Phase 7: Testing (30 min)

**Total estimated time**: 2-2.5 hours

**Note**: Website is English-only; no localization needed. All CSS styles in `site.scss` (no inline styles in .cshtml).

**Includes**:
- Complete code examples for all classes
- File paths and project structure
- Configuration examples (appsettings.json, Program.cs)
- Verification checklist
- Common issues & fixes
- Manual testing steps

### 4. Agent Context Updated ✅

Updated `CLAUDE.md` with:
- Language: C# 13 / .NET 10.0.100
- Framework: HTTP client, System.ServiceModel.Syndication, ICache service
- Storage: In-memory cache (no database)
- Project Type: ASP.NET Core vertical slice

---

## Constitution Check (Re-evaluation)

*Post-design verification*

| Principle | Status | Design Compliance |
|-----------|--------|------------------|
| **I. Vertical Slice Architecture** | ✅ PASS | Feature in `Features/Videos/` with GetVideosQuery handler; controller delegates to MediatR |
| **II. Cross-Platform Shared Contracts** | ✅ PASS | `VideoDto` in `MZikmund.DataContracts.Videos`; can be reused by Uno app |
| **III. Security by Default** | ✅ PASS | Public API (no auth needed); external read-only data; no sensitive operations |
| **IV. Code Quality & Consistency** | ✅ PASS | Follows `.editorconfig` rules; file-scoped namespaces; tab indentation; consistent naming |
| **V. Simplicity & Pragmatism** | ✅ PASS | Minimal complexity: RSS parsing + in-memory cache + display; no DB, no unnecessary abstractions |

**Gate Status**: ✅ PASS — Design is fully compliant. Ready for Phase 2 (task generation).

---

## Phase 1 Deliverables Summary

| Artifact | Location | Status | Size | Purpose |
|----------|----------|--------|------|---------|
| research.md | `/specs/001-youtube-videos/` | ✅ Complete | 8 KB | Research findings for all 5 unknowns |
| data-model.md | `/specs/001-youtube-videos/` | ✅ Complete | 9 KB | VideoDto definition, config, validation, testing data |
| videos-api.md | `/specs/001-youtube-videos/contracts/` | ✅ Complete | 12 KB | API endpoints, request/response, error handling, examples |
| quickstart.md | `/specs/001-youtube-videos/` | ✅ Complete | 20 KB | 8-phase implementation guide with complete code |
| Agent Context | CLAUDE.md (updated) | ✅ Complete | +3 KB | YouTube feature context for next session |

**Total deliverables**: 52 KB of design documentation + complete implementation guide

---

## Phase 2: Task Generation (Next Step)

After Phase 1 approval, run `/speckit.tasks` to generate:
- `tasks.md`: Actionable, dependency-ordered tasks
- GitHub issues (optional via `/speckit.taskstoissues`)

**Estimated Task Count**: 8-10 tasks covering:
1. Setup data model & configuration
2. Enhance ICache service
3. Create RSS parser
4. Create query handler
5. Create API controller
6. Create Videos page & SCSS styles
7. Create home page featured section
8. Create unit tests
9. Create integration tests
10. Manual testing & validation
