# Comments System — Design

**Date:** 2026-08-11
**Base branch:** `dev/mazi/fluent-redesign`
**Status:** Approved, ready for implementation planning

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
targets a comment that already has a parent, it is re-pointed at the grandparent and prefixed with
an `@displayname` mention. The API therefore cannot be driven into building a deep tree.

**Tombstones.** A comment deleted by its author keeps its row and its replies; `Status` becomes
`DeletedByAuthor` and the body renders as a placeholder. This prevents orphaned reply threads.

**Denormalized counts** are maintained inside the same transaction as the write that changes them,
so the post page renders from a single query.

**Limits.** A comment body is capped at 4000 characters, enforced server-side and surfaced as a
character counter in both UIs. An author cannot upvote their own comment.

**Bans** prevent future writes only. A banned author's existing comments stay visible and are hidden
individually if warranted — a ban is not a retroactive purge.

### Content handling

Comments are authored as markdown restricted to bold, italic, inline and fenced code, links,
blockquotes and lists. No images, no embeds, no raw HTML.

Rendering reuses the existing `IMarkdownConverter` (Markdig) with a restricted pipeline, followed by
`HtmlSanitizer` (Ganss.Xss). Output is written to `ContentHtml` on create and re-rendered on edit.
Links are stamped `rel="nofollow ugc"` and `target="_blank"`.

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

**Known and accepted limitation.** Because posting requires login and the API cannot reliably
distinguish the Uno app from an arbitrary HTTP client, the Turnstile requirement is enforced only for
web-form submissions and is bypassable by an authenticated caller hitting the API directly. Turnstile
is a speed bump against drive-by bot traffic, not the security boundary. Rate limiting and the ban
list are.

The Turnstile verifier sits behind an interface so it can be faked in tests.

### API surface

Public (`Controllers/CommentsController.cs`):

`sort=top` orders top-level comments by `UpvoteCount` descending then `CreatedOn` ascending;
`sort=new` orders by `CreatedOn` descending. Replies are always `CreatedOn` ascending. The response
DTO carries a `HasUpvoted` flag for the calling user so the UI can render vote state without a second
request.

```
GET    /api/posts/{postId}/comments?sort=top|new&page=   anonymous
POST   /api/posts/{postId}/comments                      auth + Turnstile (web)
PATCH  /api/comments/{id}                                auth, own comment, 15-minute window
DELETE /api/comments/{id}                                auth, own comment, soft delete
PUT    /api/comments/{id}/vote                           auth, idempotent
DELETE /api/comments/{id}/vote                           auth, idempotent
POST   /api/comments/{id}/report                         auth
```

Admin (`Controllers/Admin/CommentsAdminController.cs`, `[Authorize(Policy = "AdminPolicy")]`):

```
GET    /api/admin/comments/reports          unresolved report queue
GET    /api/admin/comments?since=           newest-first feed, drives the unread badge
POST   /api/admin/comments/{id}/hide
DELETE /api/admin/comments/{id}
POST   /api/admin/authors/{id}/ban
```

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
- `#comment-{id}` deep-links scroll to, expand and highlight the target comment.
- Without JavaScript, comments are readable and the composer degrades to a plain form post.

### App UI

- `PostView` gains a comments section driven by a new `CommentsViewModel` in `MZikmund.App.Core`,
  calling the same Refit endpoints.
- A sign-in sheet mirrors the web's three options.
- The admin section gains `CommentsModeratorView` — the report queue plus a newest-first comment feed
  with hide, delete and ban actions.
- The shell navigation carries an unread-comments badge fed by `CountNewCommentsQuery`, with the
  last-seen timestamp held in app preferences.

### Email readiness

No mail is sent in v1. To keep the future switch-on cheap:

- `CommentAuthorEntity.Email` and `NotifyOnReply` exist from the first migration, so enabling
  notifications needs no schema change and no backfill.
- The privacy policy is updated **in this work** to cover storing comment content, display name,
  avatar URL and email address, and to state that email is retained for future reply notifications.

### Configuration and rollout

New configuration keys: `Supabase:ProjectRef`, `Supabase:Url`, `Turnstile:SiteKey`,
`Turnstile:SecretKey`, `Comments:Enabled`.

`Comments:Enabled` gates the UI and every comment endpoint, read and write, so the feature can ship
dark and be switched on after a production smoke test.

## Testing strategy

Test-first, in `MZikmund.Web.Core.Tests` (xUnit + Moq, matching existing conventions):

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

## Risks

| Risk | Mitigation |
|---|---|
| `Supabase.Gotrue` is community-maintained | Small surface used; hand-rollable against the GoTrue HTTP API if needed |
| GoTrue endpoint shapes drift between versions | Verify against live docs during implementation; do not rely on training data |
| Apple Service ID / key / domain verification is fiddly and done in Apple's portal, not in code | Account confirmed available; do the Apple portal setup early in the auth phase so it is not discovered late |
| Nested `app_metadata` claim breaks `RequireClaim` | Explicit `IClaimsTransformation` step is in the plan |
| Ripping out Auth0 locks Martin out of admin | Verify the Supabase admin token end-to-end in a non-production environment before removing Auth0 config |

## Implementation phasing

This spec is deliberately large because the auth swap and the comments feature ship together. The
implementation plan should sequence it so each phase is independently verifiable:

1. Supabase auth replacing Auth0, admin login proven on every target before Auth0 config is deleted.
2. Data model, migration, and the comment MediatR handlers, test-first.
3. Public and admin API surface plus rate limiting and Turnstile verification.
4. Web UI — server-rendered partial, then `Comments.ts` hydration and write-then-auth.
5. App UI — reader/writer parity, then the moderation section and unread badge.
6. Privacy policy update and dark-launch smoke test.

## Open items to verify during implementation

- Verify GoTrue authorize/token endpoint shapes and per-platform redirect URI registration against
  live Supabase documentation rather than training data.
- Confirm the exact claim name and nesting Supabase emits for `app_metadata` in a live token before
  writing the claims transformation.
