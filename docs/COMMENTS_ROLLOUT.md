# Comments Rollout Runbook

Everything the owner does by hand to take comments (and the Supabase move that comes with them) from
this branch to production. A-C are one-off setup, D proves the hosted token, E is the Auth0 point of
no return, F-H are the dark launch, I is the rollback. Everything after the deploy is reversible.

- **App Service:** `webapp-mzikmund`, resource group `mzikmund` (per `.github/workflows/azure-publish.yml`;
  `docs/AzureNotes.md` still says `app-mzikmund`, ignore it)
- **Feature gate:** `Comments:Enabled`, as App Service setting `Comments__Enabled` (a double underscore
  is the nesting separator for every key below)
- Nothing here has been run for you. No step deploys, pushes or changes a setting by itself.

## Why the admin routes are not gated

`Comments:Enabled=false` makes the public comment routes (`api/v1/posts/{postId}/comments`,
`api/v1/comments/...`) return **404**. The moderation routes under `api/v1/admin/comments` are
deliberately left open, so moderation can be exercised in production while readers see nothing. The
privacy page and its footer link go public with the deploy, before the feature does. That is intended.

## A. Hosted Supabase project

1. Create a project at supabase.com. Note the **project ref** (the `xxxx` in `xxxx.supabase.co`) and
   the **publishable key** (`sb_publishable_...`, Settings > API Keys). The secret key goes in no app
   and no config.
2. **Settings > JWT Keys:** make an asymmetric signing key (**ES256**) the current key. HS256 tokens
   are rejected by the API. Check the key set is non-empty:
   `curl -s https://<ref>.supabase.co/auth/v1/.well-known/jwks.json` must list at least one key.
3. Check the discovery document has an absolute issuer:
   `curl -s https://<ref>.supabase.co/auth/v1/.well-known/openid-configuration` -> `issuer` is
   `https://<ref>.supabase.co/auth/v1`.
4. **Authentication > Providers:** enable **Email** (code sign-in; nobody gets a password), **Google**
   and **Apple**. Provider setup is in `docs/SUPABASE_AUTH.md` (Google: Web OAuth client; Apple:
   Services ID, key, domain verification, plus a calendar reminder because the Apple client secret
   expires every 6 months). Callback to register at both providers:
   `https://<ref>.supabase.co/auth/v1/callback`.
5. **Authentication > Emails > Magic Link:** change the template body to show `{{ .Token }}` instead of
   `{{ .ConfirmationURL }}`, otherwise users get a link and no 6-digit code. A mirror of the template
   is in `supabase/templates/magic_link.html`.
6. **Authentication > URL Configuration:** Site URL `https://mzikmund.dev`. Redirect URLs (add all):
   - `https://mzikmund.dev/**`
   - `https://mzikmund.app/**` (WASM app origin; its callback is `<origin>/authentication-callback`)
   - `https://localhost:7007/**` (local web dev against the hosted project)
   - `mzikmund-app://oauthcallback` (Windows packaged, Android, iOS)
   - `http://127.0.0.1:51842/callback` (Desktop loopback listener)
7. Sign in once so your user exists (the email-code request in D2 with `"create_user":true`), then
   grant yourself admin in the SQL editor:
   ```sql
   update auth.users
   set raw_app_meta_data = raw_app_meta_data || '{"role":"admin"}'
   where email = 'martin@zikmund.dev';
   ```
   It must be `app_metadata` (here `raw_app_meta_data`), never `user_metadata`, which users can edit.
   Tokens issued before this change do not carry the role; mint a fresh one.

## B. Cloudflare Turnstile

1. Cloudflare dashboard > Turnstile > Add widget. Domain `mzikmund.dev` (add `localhost` only if you
   want the real widget locally). Mode: Managed.
2. Copy the **site key** and the **secret key**.
3. The site key may be committed (`Turnstile:SiteKey` in `src/web/MZikmund.Web/appsettings.json`, it is
   public) or an App Service setting. The secret key is **App Service only** (`Turnstile__SecretKey`).
4. `Turnstile:ExpectedAction` already defaults to `comment-create`; leave it. Optionally set
   `Turnstile__ExpectedHostname=mzikmund.dev` to reject tokens minted for other sites.

## C. App Service configuration

The committed `appsettings.json` ships **placeholders** for Supabase and Turnstile, so either commit the
real values or set them here. Do this once, before the deploy:

```bash
az webapp config appsettings set --name webapp-mzikmund --resource-group mzikmund --settings \
  Comments__Enabled=false \
  Supabase__ProjectRef=<ref> \
  Supabase__PublishableKey=<sb_publishable_...> \
  Turnstile__SiteKey=<site-key> \
  Turnstile__SecretKey=<secret-key> \
  Turnstile__ExpectedHostname=mzikmund.dev
```

| Key | Value | Note |
|---|---|---|
| `Comments__Enabled` | `false` | pinned off so no future deploy can flip it |
| `Supabase__ProjectRef` | `<ref>` | skip if committed |
| `Supabase__PublishableKey` | `sb_publishable_...` | skip if committed; never the secret key |
| `Supabase__LocalUrl` | **not set** | dev-only; when present it overrides the project ref |
| `Turnstile__SiteKey` | site key | skip if committed |
| `Turnstile__SecretKey` | secret | App Service only |
| `Turnstile__ExpectedAction` | `comment-create` | already the default |
| `Turnstile__ExpectedHostname` | `mzikmund.dev` | optional; empty skips the check |
| `RateLimits__<Rule>__PermitLimit` / `__WindowMinutes` | optional | rules `CommentCreate` 5 per 10 min, `CommentVote` 30 per min, `CommentReport` 10 per hour, `CommentEdit` 20 per 10 min, `GlobalWrites` 20 per min per IP |

Existing settings (`ConnectionStrings__DatabaseConnection`, blob storage,
`APPLICATIONINSIGHTS_CONNECTION_STRING`) stay as they are. Verify:

```bash
az webapp config appsettings list --name webapp-mzikmund --resource-group mzikmund \
  --query "[?starts_with(name,'Comments') || starts_with(name,'Supabase') || starts_with(name,'Turnstile')].name" -o tsv
```

**Behind the Cloudflare proxy?** The rate limits key anonymous traffic on the remote IP. If the site is
(or becomes) proxied through Cloudflare, every request looks like it comes from Cloudflare and shares
one bucket. `ForwardedHeaders` (reading `CF-Connecting-IP` / `X-Forwarded-For`, trusting Cloudflare's
ranges) is **not configured** in `Program.cs` today. Azure App Service on its own is fine. Add it
before proxying.

## D. Prove the hosted admin token (the gate for anything Auth0)

The API accepts Supabase tokens only. Prove a hosted admin token works before Auth0 goes away.

1. Run the site locally against the **hosted** project with the dev local-stack override cleared
   (use Git Bash; `$env:X = ""` in PowerShell deletes the variable and lets the dev file win):
   ```bash
   cd src/web/MZikmund.Web
   export Supabase__LocalUrl= Supabase__ProjectRef=<ref> Supabase__PublishableKey=<publishable-key>
   export ConnectionStrings__DatabaseConnection='<a throwaway database, not the real one>'
   dotnet run --launch-profile https
   ```
2. Mint an admin access token (email code; password sign-in does not exist):
   ```bash
   curl -s -X POST "https://<ref>.supabase.co/auth/v1/otp" -H "apikey: <publishable-key>" \
     -H "Content-Type: application/json" -d '{"email":"martin@zikmund.dev","create_user":true}'
   curl -s -X POST "https://<ref>.supabase.co/auth/v1/verify" -H "apikey: <publishable-key>" \
     -H "Content-Type: application/json" \
     -d '{"type":"email","email":"martin@zikmund.dev","token":"<6-digit-code>"}'
   export TOKEN="<access_token from the response>"
   echo "$TOKEN" | cut -d. -f2 | base64 -d 2>/dev/null | grep -o '"role":"admin"'
   ```
3. Prove the three outcomes (`READER_TOKEN` is a token for any non-admin address):
   ```bash
   curl -sk -o /dev/null -w "%{http_code}\n" https://localhost:7007/api/v1/admin/posts                                          # 401
   curl -sk -o /dev/null -w "%{http_code}\n" -H "Authorization: Bearer $READER_TOKEN" https://localhost:7007/api/v1/admin/posts # 403
   curl -sk -o /dev/null -w "%{http_code}\n" -H "Authorization: Bearer $TOKEN" https://localhost:7007/api/v1/admin/posts        # 200
   ```
   **Stop if you do not get 401 / 403 / 200.** `401` with a good token: the project signs with HS256
   (A2), `Supabase__LocalUrl` is still set, or a key rotated in the last ~10 minutes (the JWKS cache
   lives 10 minutes). `403` for the admin: `app_metadata.role` is missing (A7); mint a fresh token.
4. Sign in from the Desktop app pointed at the hosted project
   (`MZIKMUND_SupabaseConfig__Url=https://<ref>.supabase.co`,
   `MZIKMUND_SupabaseConfig__PublishableKey=<publishable-key>`): you see the Admin menu, a reader does not.

## E. Auth0 removal (only after D passes)

Lock-out risk: after this there is no other way in. It is separate from the launch and can wait.
Plan Tasks 1.6 and 2.7 hold the exact steps; in short:

1. Web: delete the `Auth0` section from `appsettings.json`, the `Auth0.AspNetCore.Authentication`
   `PackageReference` in `MZikmund.Web.csproj`, both Auth0 `PackageVersion` lines in
   `src/Directory.Packages.props`, and `docs/AUTH0_MIGRATION.md`.
2. App: delete `AuthConfig` (the class, the objects in `appsettings.json` and the `.sample`,
   `.Section<AuthConfig>()`), any leftover `AuthenticationConstants.cs`,
   `System.IdentityModel.Tokens.Jwt` in `MZikmund.App.Core.csproj` if unused, and the
   `Auth0.OidcClient.MAUI` `PackageVersion`.
3. Fix the Auth0 mentions in `CLAUDE.md` and `README.md`; append the hosted-project section to
   `docs/SUPABASE_AUTH.md`.
4. `grep -rn Auth0 src docs CLAUDE.md README.md` finds nothing, the solution builds, tests are green.

## F. Preconditions and deploy (comments dark)

1. The committed default is off: `grep -A2 '"Comments"' src/web/MZikmund.Web/appsettings.json` shows
   `"Enabled": false`. Section C already pinned it in App Service too.
2. A-D are done. Heads-up: the deployed API accepts **only Supabase tokens**, so Auth0 sign-in in any
   old app build stops working with this deploy. Ship the new app builds around the same time.
3. **The deploy applies the `Comments` EF migration to the production database.** `Program.cs` runs
   `Database.Migrate()` on startup, so the first start after the deploy creates the comment tables
   whether or not the feature is on. Note the point-in-time restore time (or take a backup) first.
   The migration (`20261014115951_Comments`) only adds tables.
4. Merge to `main`. `.github/workflows/azure-publish.yml` builds and deploys; follow with `gh run watch`.
5. Open `https://mzikmund.dev/` and check `az webapp log tail` for migration errors.

## G. Dark smoke test (flag still false)

Mint a production admin token like D2 (against `<ref>`), then `export TOKEN=...`. `<post-id>` is a
post's GUID.

```bash
# public comment routes are dark
curl -s -o /dev/null -w "%{http_code}\n" "https://mzikmund.dev/api/v1/posts/<post-id>/comments"                                # 404
# post page renders, no composer
curl -s "https://mzikmund.dev/blog/<route-name>" | grep -c "data-comment-composer"                                              # 0
# moderation works (deliberately not gated)
curl -s -o /dev/null -w "%{http_code}\n" -H "Authorization: Bearer $TOKEN" "https://mzikmund.dev/api/v1/admin/comments"         # 200
curl -s -o /dev/null -w "%{http_code}\n" -H "Authorization: Bearer $TOKEN" "https://mzikmund.dev/api/v1/admin/comments/reports" # 200
curl -s -H "Authorization: Bearer $TOKEN" "https://mzikmund.dev/api/v1/admin/comments/count"                                    # 0
# and closed to strangers
curl -s -o /dev/null -w "%{http_code}\n" "https://mzikmund.dev/api/v1/admin/comments"                                           # 401
# privacy policy is live and linked
curl -s -o /dev/null -w "%{http_code}\n" https://mzikmund.dev/privacy                                                           # 200
curl -s https://mzikmund.dev/ | grep -c 'href="/privacy"'                                                                       # 1
```

If anything is off, stop and go to I. The feature is still dark, so only the privacy page is public.

Owner-only testing while dark: the web composer is hidden and the public write routes 404, so there is
nothing to post to yet. Test moderation through the admin routes or the Uno app's moderation page, and
do the real posting test in H right after switching on (you are the only person who knows the URL for
the first few minutes; switch off again if it misbehaves).

## H. Switch on

```bash
az webapp config appsettings set --name webapp-mzikmund --resource-group mzikmund --settings Comments__Enabled=true
```

This restarts the site. Then:

```bash
curl -s -o /dev/null -w "%{http_code}\n" https://mzikmund.dev/                                              # 200
curl -s -o /dev/null -w "%{http_code}\n" "https://mzikmund.dev/api/v1/posts/<post-id>/comments"            # 200
curl -s "https://mzikmund.dev/blog/<route-name>" | grep -c "data-comment-composer"                          # 1
```

In a real browser on a post: the composer is visible signed out; Post opens the sign-in dialog and the
draft survives the round trip; Google, Apple and email code each sign in; the comment appears; a vote
persists across a reload; delete leaves a placeholder; "By posting you agree to the privacy policy"
opens `/privacy`; the Turnstile widget passes. In the Uno app a non-admin reader must **not** see the
Admin menu.

## I. Rollback

```bash
az webapp config appsettings set --name webapp-mzikmund --resource-group mzikmund --settings Comments__Enabled=false
```

Public routes return 404 again (re-run the G check). Posted comments stay in the database and come
back when you re-enable. If the deploy itself is the problem, rerun the last good workflow:
`gh run list --workflow=azure-publish.yml --limit 5`, then `gh run rerun <run-id>`. The `Comments`
tables are additive and harmless to leave in place; restore the backup only if the migration itself
damaged something.
