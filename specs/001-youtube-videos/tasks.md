# Tasks: YouTube Videos Integration

**Feature**: 001-youtube-videos | **Branch**: `001-youtube-videos`
**Generated**: February 13, 2025 | **Status**: Ready for Implementation
**MVP Scope**: Complete User Stories 1 & 2 (all Phase 3-4 tasks) - estimated 2-2.5 hours

---

## Overview

Implementation tasks for YouTube Videos Integration feature. Organized by phase and user story for independent implementation and testing. All tasks follow vertical slice architecture (MediatR pattern) and ASP.NET Core conventions.

### Implementation Strategy

**MVP Approach**: User Stories 1 & 2 (Browse & Watch videos on dedicated Videos tab) are critical path. Story 3 (home page featured section) and Story 4 (channel link) are enhancements that add discoverability.

**Parallel Opportunities**:
- Backend setup (ICache, Parser, Query handler) can be completed in parallel with frontend (Pages, Partials)
- Tests can be developed alongside implementations
- No circular dependencies between stories

### Task Execution Order

Tasks MUST be completed in dependency order (see Dependencies section below). Within a phase, [P] marked tasks are parallelizable.

---

## Phase 1: Setup & Infrastructure (15 min)

Initial project setup: data model, configuration, service layer foundation.

### Phase 1 Goal
Create core data model and configuration infrastructure needed by all subsequent features.

### Phase 1 Independent Test Criteria
- [ ] VideoDto can be instantiated with required fields
- [ ] YouTubeOptions loads from appsettings.json
- [ ] Configuration validation throws on invalid FeedUrl at startup
- [ ] ICache interface supports async Get/Set/Remove operations

---

- [x] T001 Create VideoDto record in `src/shared/MZikmund.DataContracts/Videos/VideoDto.cs`
- [x] T002 [P] Create YouTubeOptions configuration class in `src/web/MZikmund.Web.Configuration/YouTubeOptions.cs`
- [x] T003 [P] Add YouTube configuration to `src/web/MZikmund.Web/appsettings.json`
- [x] T004 [P] Register YouTubeOptions in `src/web/MZikmund.Web/Program.cs` with startup validation
- [x] T005 Enhance ICache interface in `src/web/MZikmund.Web.Core/Services/ICache.cs` with GetAsync/SetAsync/RemoveAsync methods
- [x] T006 [P] Implement ICache with IMemoryCache in `src/web/MZikmund.Web.Core/Services/Cache.cs`

---

## Phase 2: Foundational Services (30 min)

Core backend services: RSS parser, query handler, caching logic. These are shared by all user stories.

### Phase 2 Goal
Implement data fetching, parsing, and caching infrastructure independent of UI/API layers.

### Phase 2 Independent Test Criteria
- [ ] YouTubeRssFeedParser can parse mock YouTube RSS feed without errors
- [ ] Parser returns VideoDto list in reverse chronological order
- [ ] Parser skips malformed entries and logs warnings
- [ ] GetVideosQueryHandler returns cached data on cache hit
- [ ] GetVideosQueryHandler fetches fresh data on cache miss
- [ ] GetVideosQueryHandler caches result for 30 minutes with correct TTL
- [ ] Query handler returns null gracefully on feed unavailable

---

- [ ] T007 Create YouTubeRssFeedParser in `src/web/MZikmund.Web.Core/Features/Videos/RssParsing/YouTubeRssFeedParser.cs` with ParseAsync method
- [ ] T008 [P] Register HTTPClient and YouTubeRssFeedParser in `src/web/MZikmund.Web/Program.cs`
- [ ] T009 Create GetVideosQuery in `src/web/MZikmund.Web.Core/Features/Videos/Queries/GetVideosQuery.cs`
- [ ] T010 Create GetVideosQueryHandler in `src/web/MZikmund.Web.Core/Features/Videos/Queries/GetVideosQueryHandler.cs` with caching logic

---

## Phase 3: User Story 1 - Browse Latest Videos (45 min)

Core feature: display all available videos on dedicated Videos tab. Independent from home page featured section.

### US1 Goal
Website visitors can navigate to Videos tab and see all available videos from YouTube channel in card format.

### US1 Independent Test Criteria
- [ ] Videos page loads without errors
- [ ] API endpoint `/api/videos` returns 200 OK with video list in JSON format
- [ ] Video cards display thumbnail, title, trimmed description, publication date
- [ ] Videos displayed in reverse chronological order (newest first)
- [ ] API endpoint returns empty array gracefully if no videos available
- [ ] API endpoint returns 503 if feed unavailable and no cache available

---

- [ ] T011 Create VideosController in `src/web/MZikmund.Web/Controllers/VideosController.cs` with GetVideos and GetLatestVideos endpoints
- [ ] T012 [P] Create Videos.cshtml page in `src/web/MZikmund.Web/Pages/Videos.cshtml`
- [ ] T013 [P] Create Videos.cshtml.cs page model in `src/web/MZikmund.Web/Pages/Videos.cshtml.cs`
- [ ] T014 Create _VideoListItem.cshtml partial in `src/web/MZikmund.Web/Pages/Shared/_VideoListItem.cshtml`
- [ ] T015 [P] Add Videos tab to main navigation in `src/web/MZikmund.Web/Pages/Shared/_Layout.cshtml`
- [ ] T016 [P] Add video card styles to `src/web/MZikmund.Web/wwwroot/scss/site.scss` (overlay, hover effects)

---

## Phase 4: User Story 2 - Watch Videos on YouTube (15 min)

Video cards must link directly to YouTube. Extends US1 with click functionality.

### US2 Goal
Visitors can click video cards to watch full videos on YouTube with visual feedback.

### US2 Independent Test Criteria
- [ ] Video card links resolve to correct YouTube video URL
- [ ] Video cards show hover state indicating clickability
- [ ] Links open in new tab (target="_blank")
- [ ] No broken links when clicking cards

---

- [ ] T017 [US1] Verify _VideoListItem partial includes YouTube video URL link and hover styles
- [ ] T018 [US1] Update _VideoListItem aria-label with dynamic video title
- [ ] T019 [US1] Add play icon overlay to _VideoListItem image on hover (already in T016 SCSS)

---

## Phase 5: User Story 3 - Discover Latest Videos on Home Page (20 min)

Featured section on home page: show 3 most recent videos. Leverages existing endpoint `/api/videos/latest?count=3`.

### US3 Goal
Home page visitors immediately see featured video section with latest content for discoverability.

### US3 Independent Test Criteria
- [ ] Home page loads with featured videos section
- [ ] Featured section displays exactly 3 most recent videos (or fewer if unavailable)
- [ ] Featured videos use same card design as Videos tab
- [ ] Featured section includes link to full Videos tab

---

- [ ] T020 Update Index.cshtml.cs page model to fetch featured videos in `src/web/MZikmund.Web/Pages/Index.cshtml.cs`
- [ ] T021 [P] Add featured videos section to Index.cshtml in `src/web/MZikmund.Web/Pages/Index.cshtml`
- [ ] T022 [P] Add featured-videos SCSS section to `src/web/MZikmund.Web/wwwroot/scss/site.scss`

---

## Phase 6: User Story 4 - Channel Navigation (10 min)

Links to YouTube channel for subscription and exploration.

### US4 Goal
Visitors can easily discover and access full YouTube channel from website.

### US4 Independent Test Criteria
- [ ] YouTube channel link visible and accessible on Videos page
- [ ] YouTube channel link navigates to correct URL
- [ ] Home page featured section includes link to full Videos tab

---

- [ ] T023 Verify Videos.cshtml includes prominent channel link in header and footer
- [ ] T024 [P] Verify YouTubeOptions.ChannelUrl is used in Videos page model
- [ ] T025 [US3] Verify Index.cshtml featured videos section includes "View all videos" link to `/Videos`

---

## Phase 7: Testing & Validation (30 min)

Unit tests, integration tests, manual testing, deployment verification.

### Phase 7 Goal
Comprehensive test coverage ensuring all requirements met, caching works, error handling is graceful.

### Phase 7 Independent Test Criteria
- [ ] All unit tests pass (parser, handler, service tests)
- [ ] All integration tests pass (API endpoint contracts)
- [ ] Manual testing validates Videos page displays videos
- [ ] Manual testing validates home page shows featured section
- [ ] Manual testing validates caching (second request is faster)
- [ ] Manual testing validates 503 error when feed unavailable

---

- [ ] T026 Create YouTubeRssFeedParserTests in `src/web/MZikmund.Web.Core.Tests/Features/Videos/YouTubeRssFeedParserTests.cs`
- [ ] T027 [P] Create GetVideosQueryHandlerTests in `src/web/MZikmund.Web.Core.Tests/Features/Videos/GetVideosQueryHandlerTests.cs`
- [ ] T028 [P] Create VideosControllerTests in `src/web/MZikmund.Web.Core.Tests/Controllers/VideosControllerTests.cs`
- [ ] T029 Run all tests: `cd src && dotnet test`
- [ ] T030 [P] Manual test: Navigate to `/Videos` and verify videos load
- [ ] T031 [P] Manual test: Navigate to home page and verify featured videos section
- [ ] T032 [P] Manual test: Verify cache behavior (request twice, second is faster)
- [ ] T033 [P] Manual test: Change FeedUrl to invalid URL; verify 503 error and friendly message
- [ ] T034 Verify YouTube attribution footer present on Videos page

---

## Phase 8: Polish & Cross-Cutting Concerns (10 min)

Final cleanup, accessibility, performance optimization, deployment readiness.

### Phase 8 Goal
Production-ready feature with polished UI, accessibility, and error messages.

### Phase 8 Independent Test Criteria
- [ ] All pages render without console errors
- [ ] ARIA labels present on video cards and buttons
- [ ] Empty state message displays when no videos available
- [ ] Error messages are user-friendly (not technical)
- [ ] Loading performance meets targets: cache hit <500ms, fresh fetch <3s
- [ ] CSS classes follow project naming conventions

---

- [ ] T035 Verify ARIA labels on video cards for accessibility
- [ ] T036 [P] Test empty state message when YouTube feed returns no videos
- [ ] T037 [P] Verify error messages are user-friendly (not JSON dumps)
- [ ] T038 [P] Performance check: Measure cache hit latency (<500ms) and fresh fetch (<3s)
- [ ] T039 Commit implementation: `git add . && git commit -m "feat: Add YouTube Videos integration"`

---

## Dependencies & Completion Order

```
Phase 1 (Setup) → Phase 2 (Parser & Handler)
                        ↓
        ┌───────────────┬───────────┬──────────────┬──────────────┐
        ↓               ↓           ↓              ↓              ↓
    Phase 3 (US1)   Phase 3 Tests  Phase 5 (US3)  Phase 4 (US2) Phase 6 (US4)
    (Videos Tab)     (if TDD)      (Home Page)    (Click Func)  (Channel Link)
        ↓
    Phase 7 (Testing)
        ↓
    Phase 8 (Polish)
```

### Phase Blocking

- **Phase 1 blocks**: Phase 2 (ICache, config, DTO needed)
- **Phase 2 blocks**: Phase 3, 4, 5, 6 (Parser and handler are shared by all stories)
- **Phase 3 blocks**: Phase 4, 7 (US1 base functionality; US2 adds click; tests verify both)
- **Phase 5 can run parallel**: With Phase 3 once Phase 2 complete
- **Phase 6 can run parallel**: With Phase 3-5 once Phase 2 complete
- **Phase 7 requires**: All prior phases
- **Phase 8 final**: Cleanup and polish

### Parallelizable Tasks

Tasks marked [P] can run in parallel with siblings in same phase:
- **Phase 1**: T002, T003, T004, T006 can run in parallel (setup tasks)
- **Phase 2**: T008 can run in parallel with T007-T010
- **Phase 3**: T012, T013, T015, T016 can run in parallel (frontend tasks)
- **Phase 5**: T021, T022 can run in parallel (home page setup)
- **Phase 7**: Tests can run in parallel; manual tests T030-T034 can run in parallel
- **Phase 8**: Tests T036-T038 can run in parallel

---

## Task Format Reference

All tasks follow checklist format:
```
- [ ] [TaskID] [P?] [US?] Description with file path
```

- **Checkbox**: `- [ ]` (markdown; marks task incomplete)
- **Task ID**: Sequential number (T001, T002, etc.)
- **[P]**: Optional; indicates task is parallelizable (no cross-task dependencies)
- **[US#]**: Optional; indicates task belongs to User Story (US1, US2, etc.); Story phase tasks only
- **Description**: Clear action verb + file path

Example: `- [ ] T012 [P] Create Videos.cshtml page in src/web/MZikmund.Web/Pages/Videos.cshtml`

---

## Success Criteria (All Must Pass)

- [ ] All tasks completed in dependency order
- [ ] All unit tests pass (`dotnet test`)
- [ ] Videos tab displays all videos without errors
- [ ] Home page features 3 latest videos
- [ ] Video cards link to correct YouTube URLs
- [ ] Cache hit <500ms (95% of requests); fresh fetch <3s
- [ ] 503 error handled gracefully with fallback to cached data if available
- [ ] Empty state message shows when no videos available
- [ ] YouTube attribution present on Videos page
- [ ] Code follows project conventions (vertical slice, DTOs in DataContracts, MediatR pattern)
- [ ] No Constitution violations (assessed in plan.md ✅)

---

## Performance Targets (Per FR-005, SC-005)

| Scenario | Target | Notes |
|----------|--------|-------|
| Cache hit (95% of requests) | <500ms | In-memory lookup |
| Fresh fetch from YouTube | <3s | Network + XML parsing |
| Empty response | 200ms | Minimal processing |
| Description trimming | N/A | 3-4 lines max (~300 chars) |

---

## Related Documents

- **Specification**: `specs/001-youtube-videos/spec.md` (user stories, requirements)
- **Design Plan**: `specs/001-youtube-videos/plan.md` (architecture, tech stack)
- **Data Model**: `specs/001-youtube-videos/data-model.md` (VideoDto, validation)
- **Research**: `specs/001-youtube-videos/research.md` (design decisions)
- **API Contracts**: `specs/001-youtube-videos/contracts/videos-api.md` (endpoints)
- **Quick Start Guide**: `specs/001-youtube-videos/quickstart.md` (step-by-step code)
- **Project Constitution**: `.specify/memory/constitution.md` (architectural principles)
