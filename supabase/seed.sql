-- Re-applied by `supabase db reset`. The admin test user itself is created by
-- supabase/seed-admin.ps1 through the GoTrue Admin API (a hand-written INSERT into auth.users
-- would also have to create the matching auth.identities row and several NOT-NULL-but-empty
-- token columns). This file only PATCHES the role onto that user once it exists.
--
-- Merge with || rather than assigning: GoTrue already stores
-- {"provider":"email","providers":["email"]} in this column and an overwrite breaks provider
-- bookkeeping.
update auth.users
set    raw_app_meta_data = coalesce(raw_app_meta_data, '{}'::jsonb) || '{"role": "admin"}'::jsonb
where  email = 'admin@mzikmund.test';
