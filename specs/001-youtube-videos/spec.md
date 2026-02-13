# Feature Specification: YouTube Videos Integration

**Feature Branch**: `001-youtube-videos`
**Created**: February 12, 2025
**Status**: Draft
**Input**: User description: "Add a videos tab to website which will display the latest videos from my YouTube channel"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Browse Latest Videos (Priority: P1)

Website visitors discover the Videos tab in the main navigation and view the latest published videos from the YouTube channel. Each video is presented as a card showing the thumbnail, title, trimmed description, and publication date, similar to blog cards. This allows visitors to quickly scan available video content without leaving the website.

**Why this priority**: This is the core value proposition of the feature. Without this, the feature provides no value to visitors.

**Independent Test**: Can be fully tested by navigating to the Videos tab, verifying that video cards load and display all required information, and confirms visitors can see the latest content at a glance.

**Acceptance Scenarios**:

1. **Given** a visitor on the website, **When** they click the Videos navigation tab, **Then** they see a list of video cards displaying the latest videos from the channel
2. **Given** the Videos tab is loaded, **When** a visitor views a video card, **Then** they see thumbnail, title, trimmed description, and publication date for each video
3. **Given** multiple videos are available, **When** the page loads, **Then** the videos are displayed in reverse chronological order (newest first)

---

### User Story 2 - Watch Videos on YouTube (Priority: P1)

Visitors can click on a video card to watch the full video on YouTube. Each video card links directly to the corresponding YouTube video URL, allowing viewers to engage with the full content, leave comments, and subscribe if interested.

**Why this priority**: This is essential functionality—without links to YouTube, visitors cannot watch the videos, making the feature incomplete.

**Independent Test**: Can be fully tested by clicking multiple video cards and verifying they navigate to the correct YouTube video URLs.

**Acceptance Scenarios**:

1. **Given** a visitor viewing the Videos tab, **When** they click on a video card, **Then** they are directed to the video on YouTube
2. **Given** a visitor is on a video card, **When** they hover over the card, **Then** visual feedback indicates the card is clickable (e.g., cursor change, opacity change)

---

### User Story 3 - Discover Latest Videos on Home Page (Priority: P2)

Website visitors land on the home page and immediately see a section showcasing the three most recent videos from your YouTube channel. This allows casual visitors to discover your video content without needing to navigate to a dedicated Videos tab, increasing engagement with multimedia content.

**Why this priority**: Showcasing videos on the home page increases discoverability and encourages engagement with multimedia content, but is secondary to the dedicated Videos tab.

**Independent Test**: Can be fully tested by loading the home page and verifying that a video section displays the three latest videos with proper formatting and links.

**Acceptance Scenarios**:

1. **Given** a visitor lands on the home page, **When** the page loads, **Then** they see a "Latest Videos" or similar section displaying the three most recent videos
2. **Given** the home page is displayed, **When** a visitor views the video cards in the featured section, **Then** the videos match the three latest from the Videos tab

---

Visitors can easily navigate to the full YouTube channel to see all videos, subscribe, and engage with additional channel features. A prominent link to the channel is displayed on the Videos tab, guiding interested viewers to more content. On the home page, a link allows visitors to view all videos on the Videos tab.

**Why this priority**: This drives viewers to the YouTube channel where they can subscribe and become regular audience members. Important for channel growth but secondary to viewing individual videos.

**Independent Test**: Can be fully tested by locating the channel links and verifying they navigate to the correct destinations.

**Acceptance Scenarios**:

1. **Given** a visitor on the Videos tab, **When** they look for more options, **Then** they find a clear link to the YouTube channel (e.g., "View all videos on YouTube" or "Subscribe to the channel")
2. **Given** a visitor clicks the channel link, **When** the link is activated, **Then** they are directed to the YouTube channel page
3. **Given** a visitor views the video section on the home page, **When** they want to see more videos, **Then** they find a link to the full Videos tab

---

### Edge Cases

- What happens when the YouTube RSS feed is temporarily unavailable or unreachable? → Graceful error handling with user-friendly message (FR-006); last cached videos may be displayed if available
- How does the system handle malformed or incomplete video data from the RSS feed? → Parse safely and skip malformed entries; display complete entries only
- What occurs if a video has no thumbnail or very long titles/descriptions? → Display placeholder thumbnail if missing; trim descriptions to 3-4 lines maximum (FR-007)
- What if the RSS feed returns zero videos? → Display empty state message with channel link (FR-010)

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System backend MUST fetch the latest videos from the YouTube RSS feed URL configured in `appsettings.json` (key: `YouTube:FeedUrl`), caching the results for 30 minutes before fetching fresh data from the feed (server-side operation). Default: `https://www.youtube.com/feeds/videos.xml?channel_id=UCB6Td35bzTvcJN_HG6TLtwA`
- **FR-002**: System MUST display all available video cards from the RSS feed, containing: video thumbnail, title, trimmed description (3-4 lines max, similar to blog card formatting), publication date, and clickable link to YouTube video
- **FR-003**: System MUST display videos in reverse chronological order (newest first)
- **FR-004**: System MUST parse RSS feed data to extract video title, description, thumbnail URL, publication date, and link to the YouTube video
- **FR-005**: System MUST provide a prominent, clear link to the YouTube channel (`https://www.youtube.com/@mzikmund`) on the Videos tab
- **FR-006**: System MUST handle RSS feed errors gracefully and display a user-friendly message if the feed cannot be fetched
- **FR-010**: System MUST display a user-friendly empty state message (e.g., "No videos available yet. Check back soon!") with a link to the YouTube channel if no videos are returned from the RSS feed
- **FR-011**: System MUST include YouTube branding and attribution on the Videos tab to comply with YouTube Terms of Service (e.g., "Powered by YouTube" or official YouTube logo/link)
- **FR-012**: System MUST display the three most recent videos from the YouTube RSS feed on the home page in a horizontal row/grid layout
- **FR-013**: Home page video cards MUST use the same card design and content (thumbnail, title, trimmed description, date) as the Videos tab
- **FR-014**: Home page video cards MUST link directly to the YouTube video (same behavior as Videos tab)
- **FR-015**: System MUST include a "View all videos" or similar link on the home page video section that navigates to the full Videos tab
- **FR-007**: System MUST trim video descriptions to 3-4 lines maximum (consistent with blog card description length) to maintain consistent card appearance
- **FR-008**: System MUST add a "Videos" tab to the main website navigation
- **FR-009**: System MUST automatically refresh the cached video data after 30 minutes have elapsed since the last successful fetch from the YouTube RSS feed

### Key Entities *(include if feature involves data)*

- **Video**: Represents a single YouTube video entry parsed from the RSS feed. Attributes include: title, description, thumbnail URL, publication date, YouTube video URL, channel reference. These are read-only, fetched real-time from YouTube RSS feed.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Videos load and display on the page without errors in 95% of page visits
- **SC-002**: Video cards display consistently with blog cards in terms of layout, spacing, and visual hierarchy
- **SC-003**: Visitors can successfully navigate from a video card to the YouTube video URL without broken links
- **SC-004**: The Videos tab is discoverable and accessible from the main website navigation
- **SC-005**: RSS feed data is fetched and rendered within 1 second of page load when served from cache (95% of requests); fresh fetch within 3 seconds under normal network conditions
- **SC-006**: The YouTube channel link is clearly visible and easy to locate on the Videos tab

## Clarifications

### Session 2025-02-12

- Q: What should be the maximum character count or line count for trimmed video descriptions? → A: 3-4 lines max
- Q: How many videos should be displayed when the Videos tab loads? → A: All videos from the RSS feed
- Q: What should be displayed if there are no videos available in the RSS feed? → A: Display a friendly message like "No videos available yet. Check back soon!" with a link to the YouTube channel
- Q: What type of loading indicator should display while fetching videos? → A: Server-side fetching; no client-side loading indicator needed
- Q: Should the Videos tab include YouTube attribution, brand requirements, or ToS compliance measures? → A: Include YouTube branding (e.g., "Powered by YouTube") and ensure YouTube ToS compliance
- Q: How should the three-video section appear on the home page? → A: Horizontal section with three video cards in a row/grid
- Q: Where should clicking a video card on the home page lead visitors? → A: Direct link to the YouTube video (same as Videos tab)
- Q: How should the YouTube RSS feed URL and channel URL be configured? → A: Both URLs should be stored in appsettings.json for environment-specific configuration

## Assumptions

- YouTube RSS feed URL remains accessible and stable
- Video descriptions from YouTube RSS are suitable for trimming and display as-is (no additional sanitization beyond what's standard for blog cards)
- The website already has established card component styling that can be reused for video cards
- The main website navigation structure supports adding a new "Videos" tab
- Network requests to YouTube RSS feed are the responsibility of the backend, not the client
- Video thumbnails are provided by YouTube and are HTTPS-accessible
- `appsettings.json` configuration is loaded during application startup
- Configuration values can be environment-specific (development, staging, production)
