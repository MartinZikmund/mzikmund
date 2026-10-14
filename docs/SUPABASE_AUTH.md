# Supabase authentication

Supabase is the sole identity provider for mzikmund.dev. It is used **only** for identity — no
Supabase database, storage or realtime. Comment data lives in Azure SQL behind EF Core.

## Local development prerequisites

- **Docker Desktop** (WSL2 backend on Windows), running.
- **Supabase CLI v2.71.1 or newer** — `winget install Supabase.CLI`. Older CLIs sign user access
  tokens with HS256 and the API will reject every one of them.

## The loop

```powershell
npx supabase start                 # boots the stack (first run pulls images)
pwsh .\supabase\seed-admin.ps1 # creates admin@mzikmund.test with app_metadata.role = admin
npx supabase stop                  # stops without wiping the database
npx supabase db reset              # re-runs migrations + seed.sql; re-run seed-admin.ps1 afterwards
```

Endpoints once started:

| What | URL |
|---|---|
| API / GoTrue | `http://127.0.0.1:54321` |
| Postgres | `postgresql://postgres:postgres@127.0.0.1:54322/postgres` |
| Studio | `http://127.0.0.1:54323` |
| Mailpit (mail catcher) | `http://127.0.0.1:54324` |

The local API keys are fixed constants baked into the CLI, so they are safe to commit:
`sb_publishable_ACJWlzQHlZjBrEguHvfOxg_3BJgxAaH` and `<local-secret-key>`.

## Docker is optional for anonymous browsing

`dotnet run` works with no Supabase stack running. The JWKS document is fetched lazily, on the first
token validation, so every anonymous page — including reading comments — still renders. Only
authenticated requests return 401. This is deliberate; do not "fix" it by validating configuration
at startup.

## Token shape

Local and hosted are identical in shape:

- Algorithm `ES256` (asymmetric), validated against `{base}/auth/v1/.well-known/jwks.json`.
- Issuer `{base}/auth/v1`, audience `authenticated`.
- The admin role lives in `app_metadata.role`. It must **never** live in `user_metadata`, which is
  user-editable and therefore unusable for authorization.
- ASP.NET Core surfaces `app_metadata` as a *single* claim whose value is raw JSON, so
  `RequireClaim("app_metadata", ...)` silently never matches.
  `SupabaseAppMetadataClaimsTransformation` projects the nested value into a `supabase:role` claim,
  which is what `AdminPolicy` checks.

## Getting a token for manual API testing

```powershell
$token = pwsh .\supabase\get-test-token.ps1
curl.exe -k -H "Authorization: Bearer $token" https://localhost:7007/api/v1/admin/posts
```

The script sends an email OTP, reads the 6-digit code out of Mailpit, and redeems it. Email OTP is
the only sign-in path automatable locally: Google needs a real consent screen and blocks automated
browsers, and Apple rejects localhost and IP addresses as Service ID return URLs, so Apple sign-in
must be verified against a deployed HTTPS host.

## Configuration keys

| Key | Where |
|---|---|
| `Supabase:ProjectRef` | `appsettings.json` (committed) — hosted project ref |
| `Supabase:PublishableKey` | committed; it is public by design |
| `Supabase:LocalUrl` | `appsettings.Development.json` only — `http://127.0.0.1:54321` |
| `Turnstile:SiteKey` | committed |
| `Turnstile:SecretKey` | user-secret locally, App Service setting in production |
| `Turnstile:ExpectedHostname` | optional, e.g. `mzikmund.dev`; empty skips the check (leave it empty with Cloudflare's test keys) |
| `Comments:Enabled` | committed / App Service setting |
| `RateLimits:CommentCreate` / `CommentVote` / `CommentReport` / `CommentEdit` / `GlobalWrites` | optional, each `{ PermitLimit, WindowMinutes }`; defaults are built in |

### Behind a proxy

The comment rate limits partition anonymous traffic and the global write limit by the connection's
remote IP. If the site is ever put behind the Cloudflare proxy (or any other reverse proxy), every
request will appear to come from the proxy and share one bucket, so `ForwardedHeaders` must be
configured first (`UseForwardedHeaders` with the proxy's `KnownProxies` / `KnownNetworks`, reading
`CF-Connecting-IP` / `X-Forwarded-For`). Azure App Service already sets `X-Forwarded-For`, so this
only matters once something else sits in front of it.

No Supabase **secret** key belongs in either application. It is used by hand, from
`supabase/seed-admin.ps1`, against the local stack only.

## Uno app

The app talks to GoTrue over plain HTTP (no Supabase NuGet package): PKCE code exchange, refresh,
email OTP and logout. Sessions are cached in `IAppPreferences`; a cached token whose issuer is not
`{Url}/auth/v1` is discarded. Only `app_metadata.role == "admin"` shows the Admin menu.

### Configuration

| Key | Notes |
|---|---|
| `SupabaseConfig:Url` | Full URL: `https://<ref>.supabase.co`, `http://127.0.0.1:54321` (local), `http://10.0.2.2:54321` (Android emulator) |
| `SupabaseConfig:PublishableKey` | Public by design; never a secret key |
| `SupabaseConfig:DesktopCallbackPort` | Defaults to `51842` |

The committed `appsettings.json` ships placeholders. Override per machine with
`appsettings.development.json` (see the `.sample`) or environment variables:
`MZIKMUND_SupabaseConfig__Url` and `MZIKMUND_SupabaseConfig__PublishableKey`.

### Redirect URIs per head

Every one must be in the Supabase allow-list (`additional_redirect_urls` locally, Authentication >
URL Configuration on the hosted project).

| Head | Redirect URI |
|---|---|
| Windows (packaged) | `mzikmund-app://oauthcallback` (protocol activation) |
| Desktop (Skia) | `http://127.0.0.1:51842/callback` (loopback listener) |
| Android | `mzikmund-app://oauthcallback` (`WebAuthenticationBrokerActivity`) |
| iOS | `mzikmund-app://oauthcallback` (`CFBundleURLTypes` in `Info.plist`) |
| WebAssembly | `<origin>/authentication-callback` (Uno default). Local runs are covered by the `http://localhost:5000/**` entry; add the hosted app origin when it is known |

### Local recipe

1. `npx supabase start`, then `pwsh supabase/seed-admin.ps1`.
2. Run the app with `MZIKMUND_SupabaseConfig__Url=http://127.0.0.1:54321` and the local publishable key.
3. Sign in with "Continue with email" and read the 6-digit code from Mailpit (`http://127.0.0.1:54324`).
   `admin@mzikmund.test` sees the Admin menu, `reader@mzikmund.test` does not.

Google and Apple only work end to end against the hosted project (see the provider steps the owner
performs in the dashboard: enable the providers, add the client IDs and the redirect URIs above).
