# Comments System — Design

**Date:** 2026-08-11
**Base branch:** `dev/mazi/fluent-redesign`
**Status:** Approved; revised 2026-08-11 after codebase research

> **Revision note.** The sections below were corrected against the real codebase after the initial
> approval. Substantive changes: API routes are `api/v1/…` (the original `/api/…` would have 404'd
> the Uno app); comment rendering gets its own renderer rather than reusing the app-shared
> `IMarkdownConverter`; the browser signs in via `@supabase/supabase-js` behind a new bundler, which
> makes the no-JavaScript experience read-only; and several under-specified areas (report
> resolution, unread-count route, DTO shape, deep-link paging, Turnstile enforcement predicate,
> mention storage, the author-upsert seam) are now pinned down.

## Summary

Add a comments system to mzikmund.dev supporting article comments, one level of replies, and
upvotes. Comments are readable by everyone and writable by authenticated users. Authentication
moves entirely from Auth0 to Supabase, offering Google, Apple and email sign-in. The feature is
available on both the web site and the Uno Platform app.

Comment data lives in the existing Azure SQL Server database behind EF Core. Supabase is used
**only** as an identity provider — no Supabase database, storage or realtime usage.

## Goals

- Readers can comment on any published post, reply once deep, and upvote comments.
- Signing in is as frictionless as possible: Google, Apple, or an email code — never a password.
- The Uno app has full reader/writer parity with the web.
- Martin can moderate (hide, delete, ban) and see what is new, from the app.
- Spam and abuse are controlled server-side.
- The design is ready for reply-notification emails to be switched on later without a migration.

## Non-goals (v1)

- **No email sending.** Notification plumbing is not built; the schema and privacy policy are
  prepared for it. See "Email readiness".
- **No realtime/live-updating comments.** Pages fetch on load and after a write.
- **No rich media in comments** — no image upload, no embeds, no raw HTML.
- **No user profile pages** or public comment history.
- **No comment search.**
- **No posting without JavaScript.** Sign-in runs in the browser via `@supabase/supabase-js`, so
  with JS disabled comments are readable but the composer is hidden. This is a deliberate change
  from the original draft, which promised a plain-form-post fallback that cannot authenticate.

## Decisions and rationale

| Decision | Choice | Why |
|---|---|---|
| Identity provider | Supabase, replacing Auth0 entirely | Auth0 was a quick placeholder with exactly one user (admin), so there is nothing to migrate. One issuer beats two. |
| Sequencing | Single combined spec (auth swap + comments) | With no users to migrate, the auth swap is a rip-and-replace, not a migration project. |
| Comment storage | Existing SQL Server via EF Core | Real FK to `PostEntity`, existing MediatR/EF patterns, one backup story, and server-side enforcement of captcha, rate limits and moderation. |
| Threading depth | One level | Reads well at 360px, single-fetch query, no indentation slivers. |
| Moderation | Post-hoc — comments appear immediately | Login plus captcha already suppresses drive-by spam; nobody waits on a queue. |
| App scope | Full reader/writer parity; moderation only in the app | Keeps the web free of admin-conditional rendering. |
| Notifications | None in v1; unread badge in the app | No email service, secret, or GDPR surface in the request path yet. |
| Captcha | Turnstile on web + rate limits on every surface | See "Abuse control" for the honest limits of this. |

## Architecture

### Authentication

Auth0 is removed completely:

- the `Auth0` configuration section and its placeholder validation in `Program.cs`
- `src/app/MZikmund.App.Core/Services/Account/AuthenticationConstants.cs`
- the `#if WINDOWS` `OAuth2Manager` path and the commented-out non-Windows path in `UserService.cs`
- `docs/AUTH0_MIGRATION.md`, replaced by `docs/SUPABASE_AUTH.md`

Supabase replaces it as the sole issuer.

**Providers enabled:** Google, Apple, and email OTP (6-digit code, not magic link — a code works
identically in a native app and avoids a round trip through a mail client). Password auth stays
disabled.

Apple sign-in ships in v1. Martin holds a paid Apple Developer account, so the plan includes creating
the Service ID and signing key and verifying the domain with Apple. This also keeps the iOS build
compliant with Apple's requirement that Sign in with Apple be offered wherever other social logins
are.

**API validation.** Supabase signs access tokens with ES256. The API validates against the project
JWKS endpoint rather than a shared secret:

- JWKS: `https://<project-ref>.supabase.co/auth/v1/.well-known/jwks.json`
- Issuer: `https://<project-ref>.supabase.co/auth/v1`
- Audience: `authenticated`

A single `AddJwtBearer` registration replaces the Auth0 one, validating issuer, audience, lifetime
and signing key.

**Admin authorization.** The admin role lives in `app_metadata.role = "admin"`. It must **not** live
in `user_metadata`, which is user-editable in Supabase and therefore unsafe for authorization
decisions.

Implementation note: ASP.NET Core surfaces `app_metadata` as a single claim whose value is raw JSON,
so `RequireClaim` cannot read the nested role directly. `AdminPolicy` needs an `IClaimsTransformation`
that parses `app_metadata` and projects `role` into a first-class claim. This is a known trap; it is
called out here so the plan budgets for it.

**App sign-in.** PKCE authorization-code flow via Uno's `WebAuthenticator` on iOS, Android and WASM,
and the existing Windows flow, driven by the community-maintained `Supabase.Gotrue` client
(NuGet package `supabase`). Community maintenance is a known risk accepted here; the surface used is
small (authorize URL construction, code exchange, refresh) and can be hand-rolled against the GoTrue
HTTP API if the package becomes a problem.

The exact GoTrue endpoint shapes and redirect URI registration per platform must be verified against
live Supabase docs during implementation — they are not stable across versions.

### Local user projection

A `CommentAuthorEntity` in the application database, keyed by the Supabase `sub` GUID:

| Column | Notes |
|---|---|
| `Id` (PK) | Supabase `sub` |
| `DisplayName` | from JWT claims |
| `AvatarUrl` | from JWT claims, nullable |
| `Email` | from JWT `email` claim — stored for future notifications |
| `IsBanned` | moderation |
| `NotifyOnReply` | defaults `true`, unused in v1 |
| `CreatedOn`, `LastSeenOn` | |

The row is upserted from JWT claims on every authenticated write. Author identity is **never** read
from the request body.

### Data model

```
CommentEntity
  Id (Guid, PK)
  PostId       FK -> PostEntity
  ParentId     FK -> CommentEntity (null = top-level)
  AuthorId     FK -> CommentAuthorEntity
  Content      nvarchar(max)   raw markdown as authored
  ContentHtml  nvarchar(max)   rendered + sanitized, produced on write
  Status       enum: Visible | DeletedByAuthor | HiddenByModerator
  CreatedOn, EditedOn?
  UpvoteCount, ReplyCount, ReportCount   denormalized

CommentVoteEntity
  (CommentId, AuthorId) composite PK, CreatedOn

CommentReportEntity
  Id, CommentId, ReporterId, Reason, CreatedOn, ResolvedOn?
```

Indexes: `(PostId, ParentId, CreatedOn)` for the post page, `(AuthorId)` for ban enforcement and
rate limiting.

`CommentReportEntity.Reason` is an enum — `Spam`, `Abuse`, `OffTopic`, `Other` — with an optional
free-text note capped at 500 characters.

Fluent API configurations go in `MZikmund.Web.Data/Entities/Configurations/`, matching the existing
convention. Delete behaviour is restricted — comments are soft-deleted via `Status`, never removed
by cascade.

**Threading invariant.** One level is enforced in the handler, not merely in the UI: if a reply
targets a comment that already has a parent, it is re-pointed at the grandparent and attributed to
the comment it was actually aimed at. The API therefore cannot be driven into building a deep tree.

The attribution is stored as a nullable `MentionedDisplayName` column rendered by the UI as an
`@name` prefix — **not** baked into `Content`. Keeping `Content` exactly as the author typed it means
the edit round-trip cannot double-prefix, and the 4000-character cap stays honest. The same rule
applies on edit: re-pointing is decided once at creation and never re-derived.

**Tombstones.** A comment deleted by its author keeps its row and its replies; `Status` becomes
`DeletedByAuthor` and the body renders as a placeholder. This prevents orphaned reply threads.

**Denormalized counts** are maintained inside the same transaction as the write that changes them,
so the post page renders from a single query.

**Limits.** A comment body is capped at 4000 characters, enforced server-side and surfaced as a
character counter in both UIs. An author cannot upvote their own comment.

**Bans** prevent future writes only. A banned author's existing comments stay visible and are hidden
individually if warranted — a ban is not a retroactive purge.

### Data access and the write seam

Comment handlers inject `DatabaseContext` directly rather than the `IRepository<T>` abstraction the
Posts/Categories features use. `IRepository<T>` calls `SaveChangesAsync` inside each operation, so it
cannot write a comment and its denormalized `UpvoteCount`/`ReplyCount` in one transaction. Any
explicit transaction must be wrapped in `Database.CreateExecutionStrategy().ExecuteAsync(...)`,
because the context is configured with `EnableRetryOnFailure` and throws otherwise.

Two concerns cut across every authenticated write — upserting the author row from JWT claims, and
rejecting banned authors. Rather than repeating them in five handlers, they live in a MediatR
`IPipelineBehavior` constrained to an `IAuthoredRequest` marker interface, reading claims via
`IHttpContextAccessor`. This is new infrastructure for this codebase and is called out so the plan
budgets for it and the tests can fake one seam instead of five.

### Content handling

Comments are authored as markdown restricted to bold, italic, inline and fenced code, links,
blockquotes and lists. No images, no embeds, no raw HTML.

Rendering does **not** reuse the existing `IMarkdownConverter`, contrary to the original draft. That
converter lives in `MZikmund.Shared` (shipped into the Uno app), its pipeline is deliberately
permissive, and `PostContentProcessor` injects raw `<script>` gist embeds — none of which is
acceptable for user-generated content. Instead a new `ICommentContentRenderer` in
`MZikmund.Web.Core` builds its own restricted Markdig pipeline and runs the output through
`HtmlSanitizer` (Ganss.Xss) with an allow-list matching exactly what that pipeline can emit.

Output is written to `ContentHtml` on create and re-rendered on edit. Links are stamped
`rel="nofollow ugc noopener noreferrer"` and `target="_blank"`.

Sanitization runs server-side on write. The client never sanitizes and never trusts client-supplied
HTML.

### Abuse control

Three layers, in order of actual effectiveness:

1. **Authentication.** Posting and voting require a signed-in Supabase user.
2. **Rate limiting.** ASP.NET Core's built-in rate limiting middleware (`AddRateLimiter`), partitioned
   on the authenticated user id and falling back to client IP for anonymous reads. Starting limits,
   tunable via configuration: 5 comment creates per 10 minutes, 30 votes per minute, and 10 reports
   per hour per user; 20 writes per minute per IP. Enforced on every surface, web and app alike.
   This is the real control.
3. **Cloudflare Turnstile** on the web comment form, verified server-side against the siteverify
   endpoint.

   **Enforcement predicate**, which the original draft left undefined: the token travels in a
   `CF-Turnstile-Response` header (keeping it out of the shared DTO so the app carries no dead
   field). *If a token is present it MUST verify*, including the `action` echo check, or the write
   is rejected. Absence is permitted and logged — otherwise the Uno app could never post. This makes
   the "a failing verification rejects the write" test meaningful while keeping the app path open.
   Creates are gated; edits and reports are not.

**Known and accepted limitation.** Because posting requires login and the API cannot reliably
distinguish the Uno app from an arbitrary HTTP client, the Turnstile requirement is enforced only for
web-form submissions and is bypassable by an authenticated caller hitting the API directly. Turnstile
is a speed bump against drive-by bot traffic, not the security boundary. Rate limiting and the ban
list are.

The Turnstile verifier sits behind an interface so it can be faked in tests.

### API surface

Public (`Controllers/CommentsController.cs`):

**Route prefix.** Every existing controller uses `api/v1/…` and the Refit client targets `/v1/…`
against a base address ending in `/api`. Comment routes follow that convention — the original
`/api/…` form in this spec was wrong and would have 404'd the Uno app.

**Response shape.** `GET` returns `PagedResponse<CommentDto>` where `Data` holds **top-level comments
only** and `TotalCount` is the top-level count. Each `CommentDto` carries a bounded `Replies`
array (first 3, `CreatedOn` ascending) plus a full `ReplyCount`, so the UI renders "+N more replies"
and fetches the remainder on demand. `sort=top` orders top-level by `UpvoteCount` descending then
`CreatedOn` ascending; `sort=new` orders by `CreatedOn` descending. Replies are always `CreatedOn`
ascending regardless of `sort`. Each DTO carries `HasUpvoted` for the calling user so vote state
needs no second request.

```
GET    /api/v1/posts/{postId}/comments?sort=top|new&pageNumber=   anonymous
GET    /api/v1/comments/{id}/replies?pageNumber=                  anonymous, reply overflow
POST   /api/v1/posts/{postId}/comments                            auth (+ Turnstile, see below)
PATCH  /api/v1/comments/{id}                    auth, own comment, 15-minute window
DELETE /api/v1/comments/{id}                    auth, own comment, soft delete
PUT    /api/v1/comments/{id}/vote               auth, idempotent
DELETE /api/v1/comments/{id}/vote               auth, idempotent
POST   /api/v1/comments/{id}/report             auth
```

Admin (`Controllers/Admin/CommentsAdminController.cs`, `[Authorize(Policy = "AdminPolicy")]`):

```
GET    /api/v1/admin/comments/reports            open report queue, grouped by comment
POST   /api/v1/admin/comments/reports/{id}/resolve
GET    /api/v1/admin/comments?since=             newest-first moderation feed
GET    /api/v1/admin/comments/count?since=       unread badge count
POST   /api/v1/admin/comments/{id}/hide
DELETE /api/v1/admin/comments/{id}
POST   /api/v1/admin/authors/{id}/ban
```

`HideCommentCommand` and `DeleteCommentCommand` auto-resolve every open report on that comment in
the same transaction, so moderating a comment clears its queue entry. `ResolveCommentReportCommand`
exists for the other case: a report the moderator reviews and dismisses without touching the
comment. Without it the queue could never be emptied.

**Handler naming** follows the dominant repo convention `{Verb}{Noun}Handler` — `GetPostCommentsHandler`,
not `GetPostCommentsQueryHandler`. Controllers inject `IMediator` (there is no `ISender` anywhere in
the repository, contrary to the original draft).

**Serializer registration.** `MZikmundApiSerializerContext` must gain an entry for every new generic
—`CommentDto`, `CommentDto[]`, `PagedResponse<CommentDto>`, and the `ApiResponse<…>` wrappers. A
missing registration fails at runtime, not compile time.

MediatR handlers live in `MZikmund.Web.Core/Features/Comments/` and are picked up by the existing
assembly scan: `GetPostCommentsQuery`, `CreateCommentCommand`, `UpdateCommentCommand`,
`DeleteCommentCommand`, `SetCommentVoteCommand`, `ReportCommentCommand`, `HideCommentCommand`,
`BanAuthorCommand`, `GetCommentReportsQuery`, `CountNewCommentsQuery`.

DTOs go in `MZikmund.DataContracts/Blog/Comments/`; the Refit surface is a new partial
`IMZikmundApi.Comments.cs`, so web and app consume one definition.

### Web UI

A `_Comments.cshtml` partial renders below the article in `Pages/Blog/Post.cshtml`.

**Server-rendered on first paint** so comments appear in the HTML for crawlers and remain readable
without JavaScript, then hydrated by a new `Scripts/Comments.ts` module alongside the existing
`Motion.ts` / `TableOfContents.ts` modules, with styles following `docs/design-system.md`.

**Write-then-auth.** The composer is always visible and enabled, signed in or not. The reader types,
presses Post, and only then is sign-in requested: the draft is written to `sessionStorage`, survives
the OAuth redirect, and is auto-submitted on return. Nobody is asked to authenticate before they know
they have something to say.

Sign-in is a Fluent dialog offering Google, Apple, or email; the email option swaps inline to a
6-digit code field.

Other behaviour:

- Sort toggle `Top | Newest` on top-level comments; replies are always chronological.
- 20 top-level comments per page with a "Load more" control.
- Upvotes apply optimistically and roll back on failure.
- Deep-linking uses `/blog/{routeName}?comment={id}`: the PageModel resolves which page contains
  that comment and server-renders it directly, keeping `#comment-{id}` purely for scrolling and
  highlight. A pure fragment cannot work — fragments are never sent to the server, so a comment on
  page 3 would not be in the first paint. This is the only form that also works for crawlers.
- Without JavaScript, comments are readable but the composer is hidden. Sign-in requires JS.

**Build change.** The browser uses the official `@supabase/supabase-js` client, which the current
build cannot consume — `tsconfig` is `module: System` with `outFile`, and there are no runtime npm
dependencies. A bundler is therefore introduced and the six existing TypeScript modules move to ES
modules as part of this work. That is a real, separable task, not a footnote, and it touches the
whole site's asset pipeline including CI.

**Session storage note.** `supabase-js` persists its session client-side, so an XSS bug in the site
becomes an account-takeover bug. The client is configured for the PKCE flow explicitly, and the
comment rendering path is the main untrusted-input surface — which is why sanitization is
server-side, on write, with an allow-list.

### App UI

- `PostView` gains a comments section driven by a new `CommentsViewModel` in `MZikmund.App.Core`,
  calling the same Refit endpoints.
- A sign-in sheet mirrors the web's three options.
- The admin section gains `CommentsModeratorView` — the report queue plus a newest-first comment feed
  with hide, delete and ban actions.
- The shell navigation carries an unread-comments badge fed by `GET /api/v1/admin/comments/count`,
  with the last-seen timestamp held in app preferences. It refreshes on app launch and on navigation
  to the admin section — there is no timer and no realtime.
- **Admin gating regression to fix:** the app's Admin navigation section is currently shown whenever
  `IsLoggedIn` is true. Once readers can sign in, that would expose the admin UI to everyone, so
  `WindowShellViewModel` gains an `IsAdmin` flag driven by the `app_metadata.role` claim, and the
  Admin section binds to that instead. The API would still reject them, but the UI must not offer it.

**Comment body rendering.** Comment markdown is rendered natively with the Community Toolkit
`MarkdownTextBlock`, which depends on `RichTextBlock` support arriving in Uno 7.0 while the repo is
currently on `Uno.Sdk 6.7.0-dev.48`. The app phase is therefore structured so this is the *last*
step and does not block anything else: comment bodies render as plain text with tappable links until
the Uno bump lands, then the template swaps. If 7.0 slips, everything else still ships.

### Email readiness

No mail is sent in v1. To keep the future switch-on cheap:

- `CommentAuthorEntity.Email` and `NotifyOnReply` exist from the first migration, so enabling
  notifications needs no schema change and no backfill.
- The privacy policy is updated **in this work** to cover storing comment content, display name,
  avatar URL and email address, and to state that email is retained for future reply notifications.

  This is larger than it sounds: `Pages/Privacy.cshtml` is still the default ASP.NET template stub
  ("Use this page to detail your site's privacy policy"), so this means writing a real policy rather
  than amending one. It follows the house structure already established by the genuine app policies
  under `Pages/Apps/`. Disclosures required: identity data from Supabase and from Google/Apple
  sign-in; display name and avatar URL; email retained for not-yet-enabled reply notifications;
  comment content retention including soft-deleted tombstones; the ban list; IP addresses processed
  for rate limiting; Cloudflare Turnstile as a third-party processor that sets client-side state; and
  the client-side session storage `supabase-js` creates — which must also be reconciled with the
  site's existing cookie-consent implementation.

### Configuration and rollout

Every URL is derived from the project ref, so `Supabase:Url` is dropped as a redundant second source
of truth. Both client surfaces additionally need the publishable (anon) key for the `apikey` header
on GoTrue calls — missing from the original draft. No secret key belongs in either application; it is
used once, by hand, to grant the admin role.

| Key | Surface | Where it lives |
|---|---|---|
| `Supabase:ProjectRef` | web | committed `appsettings.json` |
| `Supabase:PublishableKey` | web | committed (it is public by design) |
| `Turnstile:SiteKey` | web | committed |
| `Turnstile:SecretKey` | web | user-secret locally, App Service setting in production |
| `Comments:Enabled` | web | committed / App Service setting |
| `SupabaseConfig` section (`ProjectRef`, `PublishableKey`) | Uno app | embedded `appsettings.json` |

The app binds configuration sections **by type name** (`.Section<SupabaseConfig>()` binds a JSON
object literally named `SupabaseConfig`), so the app's key names cannot mirror the web's `Supabase:`
section. The app's `appsettings.json` is an embedded resource — whatever it contains ships inside the
binary, which is fine for the publishable key and unacceptable for anything else.

**Feature gate.** `Comments:Enabled` binds to a `CommentsOptions` POCO in `MZikmund.Web.Configuration`
and is enforced by a single MVC action filter returning **404** on the public comment routes, plus a
server-side short-circuit in the Razor partial. Admin routes are deliberately **not** gated, so the
production smoke test can exercise moderation while the feature is dark. The Uno app has no
visibility of the flag and simply shows an empty section while dark — an accepted limitation rather
than a capability-probe endpoint.

## Testing strategy

**Test database.** Existing tests substitute the database by mocking `IRepository<T>` and never touch
`DatabaseContext` — but comment handlers use `DatabaseContext` directly, and a mocked `IQueryable`
cannot back EF's async operators. This work therefore establishes the repository's first DB-backed
tests: `Microsoft.EntityFrameworkCore.Sqlite` is added to `src/Directory.Packages.props` and the test
project, with a `CommentsDbFixture` providing a fresh schema per test class. SQLite rather than
InMemory specifically because the vote-idempotency test needs the composite primary key actually
enforced. There is no existing fixture or base class to copy — this is new ground and is called out
as its own task rather than discovered mid-implementation.

Test-first, in `MZikmund.Web.Core.Tests` (xUnit + Moq, matching existing conventions). Test classes
are named after the handler, e.g. `CreateCommentHandlerTests`:

- **Threading** — a reply to a reply is re-pointed to the grandparent and mention-prefixed.
- **Votes** — repeated `PUT` is idempotent; `DELETE` removes; counts stay consistent.
- **Edit window** — an edit inside 15 minutes succeeds, outside fails, and another user's comment
  always fails.
- **Deletion** — a deleted parent with replies becomes a tombstone and keeps its children.
- **Bans** — a banned author cannot create, edit or vote.
- **Sanitization** — a corpus of XSS payloads (script tags, `javascript:` URLs, event handlers,
  malformed markup) produces inert HTML.
- **Rate limiting** — writes past the limit are rejected.
- **Turnstile** — the verifier interface is faked; a failing verification rejects the write.
- **Authorization** — every admin route rejects a non-admin token and every write route rejects an
  anonymous one.

Plus a Playwright pass on the rendered post page per `CLAUDE.md`, covering the write-then-auth draft
restore, optimistic upvote rollback, and comment deep-linking.

**Local auth for tests.** Those flows need a signed-in session, so development and Playwright run
against a local Supabase stack (`supabase start`) with a seeded test user, completing sign-in through
the email-OTP path by reading the code from the local mail catcher — no real Google or Apple consent
screen involved. Turnstile switches to its documented always-pass/always-fail test keys in the same
environment. Note the trap this creates: a local stack signs tokens with a fixed HS256 secret while
the hosted project uses asymmetric ES256 via JWKS, so `AddJwtBearer` must be environment-conditional.
Docker becomes a development prerequisite.

## Risks

| Risk | Mitigation |
|---|---|
| `Supabase.Gotrue` is community-maintained | Small surface used; hand-rollable against the GoTrue HTTP API if needed |
| GoTrue endpoint shapes drift between versions | Verify against live docs during implementation; do not rely on training data |
| Apple Service ID / key / domain verification is fiddly and done in Apple's portal, not in code | Account confirmed available; do the Apple portal setup early in the auth phase so it is not discovered late |
| Nested `app_metadata` claim breaks `RequireClaim` | Explicit `IClaimsTransformation` step is in the plan |
| Ripping out Auth0 locks Martin out of admin | Verify the Supabase admin token end-to-end in a non-production environment before removing Auth0 config |
| Uno 7.0 (needed for `MarkdownTextBlock`) may not be released when the app phase arrives | Phase 7 is last and independent; comment bodies render as plain text until the bump lands |
| Introducing a bundler changes the asset pipeline for the entire site, not just comments | Phase 4 is standalone and must prove the site renders identically before any comments UI exists |
| `supabase-js` persists the session client-side, so XSS becomes account takeover | Server-side sanitization on write with a strict allow-list; the comment body is the main untrusted surface |
| Readers signing in would expose the app's Admin nav, which is gated only on `IsLoggedIn` | Explicit `IsAdmin` flag in phase 1, before any reader can obtain a token |
| First DB-backed tests in the repo — no fixture exists to copy | Phase 0 builds `CommentsDbFixture` before any handler test is written |

## Implementation phasing

This spec is deliberately large because the auth swap and the comments feature ship together. The
implementation plan should sequence it so each phase is independently verifiable:

0. **Local Supabase stack + test database foundation.** `supabase start`, the seeded admin user, the
   environment-conditional `AddJwtBearer`, and the `CommentsDbFixture` establishing DB-backed tests.
   Nothing else can be verified without these, so they come first.
1. **Supabase auth replacing Auth0**, admin login proven on every target *before* Auth0 config is
   deleted, plus the `IsAdmin` gating fix in the app shell.
2. **Data model, migration, and the comment MediatR handlers**, test-first, including the
   `IAuthoredRequest` pipeline behavior and `ICommentContentRenderer`.
3. **Public and admin API surface** plus rate limiting, Turnstile verification, the `Comments:Enabled`
   filter, and serializer-context registrations.
4. **Web build migration to a bundler** — ES modules, new config, CI. Separable and independently
   verifiable: the site must look and behave identically before any comments UI is added.
5. **Web comments UI** — server-rendered partial, then hydration, sign-in dialog and write-then-auth.
6. **App UI** — reader/writer parity with plain-text bodies, then the moderation section and unread
   badge.
7. **Uno 7.0 bump + `MarkdownTextBlock`** swap. Deliberately last, so a slip in Uno 7.0 blocks
   nothing.
8. **Privacy policy** and dark-launch smoke test.

## Open items to verify during implementation

- Verify GoTrue authorize/token endpoint shapes and per-platform redirect URI registration against
  live Supabase documentation rather than training data.
- Confirm the exact claim name and nesting Supabase emits for `app_metadata` in a live token before
  writing the claims transformation.
