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
| `Comments:Enabled` | committed / App Service setting |

No Supabase **secret** key belongs in either application. It is used by hand, from
`supabase/seed-admin.ps1`, against the local stack only.
