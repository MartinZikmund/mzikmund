# When you return: setting up the redesign and comments

Start here. This page tells you the order; the detailed steps live in the linked docs.

## What is waiting for you

Two pull requests, stacked:

| PR | Branch | Contents |
|---|---|---|
| 1 | `dev/mazi/appredesign` → `main` | Uno 7 migration, app Fluent redesign, working post editor, app sign-in fixes, small web fixes |
| 2 | `worktree-comments` → `dev/mazi/appredesign` | Comments system, Supabase replaces Auth0 (web + app), esbuild asset pipeline, privacy policy |

Merge PR 1 first. PR 2 is based on it, so GitHub retargets PR 2 to `main` once PR 1 is merged.

**Nothing was deployed and no hosted account was touched.** No real secrets are committed: production
values are placeholders, and the only keys in the repo are the Supabase CLI's public local publishable
key and Cloudflare's published Turnstile test keys. The local Supabase secret key is read at runtime
(`npx supabase status -o env`), never committed.

## 1. PR 1: the redesign (about an hour)

1. Review it and run the app locally (see *Local development* below). On Windows the packaged head
   needs the Windows App Runtime 2.x (Uno 7 builds against WindowsAppSDK 2.x).
2. Merge it. The web deploy carries two fixes the app relies on:
   - the admin post list reports Published/Draft;
   - the post editor sees the real published state (before, every post loaded as a draft).
3. App sign-in on this PR is still **Auth0**. If you want desktop sign-in before PR 2, add
   `http://127.0.0.1:51842/callback` to the Auth0 app's allowed callbacks. Otherwise skip it: PR 2
   replaces Auth0.

## 2. PR 2: comments and Supabase (an evening, plus provider setup)

Follow [`docs/COMMENTS_ROLLOUT.md`](COMMENTS_ROLLOUT.md) top to bottom. In short:

1. **A, B, C:** create the hosted Supabase project (ES256 signing, Email + Google + Apple providers,
   6-digit email template, redirect URLs, your `app_metadata.role = admin`), create the Turnstile
   widget, set the App Service settings with `Comments__Enabled=false`.
2. **D:** prove a hosted admin token gets 401 / 403 / 200 locally. Do not continue until it does.
3. **F:** merge PR 2 and let it deploy. The first start creates the comment tables (the site
   auto-migrates), so note a restore point first. From this deploy on, the API accepts **only
   Supabase tokens**: old app builds using Auth0 stop signing in, so release the new app builds
   (step 3) around the same time.
4. **G:** dark smoke test (public comment routes 404, moderation works, `/privacy` is live).
5. **H:** switch `Comments__Enabled=true`, do the browser pass. **I** is the rollback.
6. **E (later, optional):** remove Auth0 leftovers once you are happy. It is a cleanup, not a launch
   step.

Provider-specific setup (Google OAuth client, Apple Services ID and the 6-month Apple secret reminder)
is in [`docs/SUPABASE_AUTH.md`](SUPABASE_AUTH.md).

## 3. App release

Build and publish the app heads after PR 2 is live. Check on a real device:

- Google and Apple sign-in on each platform (redirect back via `mzikmund-app://oauthcallback`;
  desktop uses the loopback `http://127.0.0.1:51842/callback`; WASM uses its own origin);
- a reader does **not** see the Admin menu, you do;
- the comments panel next to a post (wide) and as an overlay (narrow).

## Local development

- **Docker Desktop** (installed) must be running for the local Supabase stack.
- **Supabase CLI** is used through npx, from the repo root:
  `npx supabase start` / `npx supabase stop`. After a `npx supabase db reset`, re-run
  `pwsh supabase/seed-admin.ps1`. Test tokens: `pwsh supabase/get-test-token.ps1 -Email admin@mzikmund.test`.
  Mailpit (where 6-digit codes arrive) is at http://127.0.0.1:54324.
- **Web:** `cd src/web/MZikmund.Web && dotnet run --launch-profile https` serves https://localhost:7007.
  The site **auto-migrates its database on startup**. To try the comments branch without touching
  your dev data, point it at a throwaway database:
  `ConnectionStrings__DatabaseConnection="Server=(localdb)\MSSQLLocalDB;Database=MZikmundDb_Scratch;Trusted_Connection=True;MultipleActiveResultSets=true"`.
- **App:** any config value can be overridden with `MZIKMUND_`-prefixed environment variables, e.g.
  `MZIKMUND_AppConfig__ApiUrl=https://localhost:7007/api`,
  `MZIKMUND_SupabaseConfig__Url=http://127.0.0.1:54321`,
  `MZIKMUND_SupabaseConfig__PublishableKey=<local publishable key from docs/SUPABASE_AUTH.md>`.

## Not verified (needs you, a device or real accounts)

- Google and Apple sign-in, and the redirect back into the app on Windows, Android, iOS and WASM.
- Turnstile with real keys, and the widget following a theme switch.
- High Contrast mode for comment text in the app.
- App sign-in tokens are stored in local (not roaming) settings, but not encrypted.
- In the desktop app, the comment overflow menus closed after about a second when driven by
  automation. Worth a manual click-through.

## Uno Platform findings from this work

Recorded in `C:\Users\Martin\.claude\uno-issues\` (`/mzskills:uno-issues` lists and files them):
custom fonts and synthetic bold/italic in inline runs on Skia, blank lines in `TextBlock` ignoring
font size on Skia, the Hot Design 2.0.0-dev.2 WinAppSDK Debug build break, and the wrong
`NativeApplication` namespace in the Uno 7 migration guide.
