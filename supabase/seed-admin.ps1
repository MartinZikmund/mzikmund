#requires -Version 7
<#
.SYNOPSIS
	Creates the local admin test user and grants it app_metadata.role = "admin".
.DESCRIPTION
	Uses the GoTrue Admin API rather than a raw INSERT into auth.users: a hand-written INSERT must
	also create the matching auth.identities row and populate several NOT-NULL-but-empty token
	columns. Safe to re-run - an existing user comes back as 422 email_exists, which is tolerated.
	Run this after every `supabase db reset`, because a reset wipes the auth schema too.
#>
[CmdletBinding()]
param(
	[string]$Email = "admin@mzikmund.test",
	[string]$SupabaseUrl = "http://127.0.0.1:54321",
	[string]$SecretKey = "<local-secret-key>"
)

$ErrorActionPreference = "Stop"

$headers = @{
	apikey        = $SecretKey
	Authorization = "Bearer $SecretKey"
}

$createBody = @{
	email         = $Email
	email_confirm = $true
	# app_metadata is authoritative and NOT user-editable. user_metadata would be unsafe here.
	app_metadata  = @{ role = "admin" }
	user_metadata = @{ full_name = "Martin Zikmund" }
} | ConvertTo-Json -Depth 5

try
{
	$created = Invoke-RestMethod -Method Post -Uri "$SupabaseUrl/auth/v1/admin/users" `
		-Headers $headers -ContentType "application/json" -Body $createBody
	Write-Output "Created $Email with id $($created.id)"
}
catch
{
	if ($_.Exception.Response.StatusCode.value__ -ne 422)
	{
		throw
	}

	Write-Output "$Email already exists - patching app_metadata instead."

	$users = Invoke-RestMethod -Method Get -Uri "$SupabaseUrl/auth/v1/admin/users" -Headers $headers
	$existing = $users.users | Where-Object { $_.email -eq $Email } | Select-Object -First 1
	if ($null -eq $existing)
	{
		throw "Supabase reported $Email as existing but did not list it."
	}

	# Send ONLY app_metadata. The same endpoint also accepts a top-level "role" field which
	# rewrites the Postgres role claim and is a real way to lock the account out.
	$patchBody = @{ app_metadata = @{ role = "admin" } } | ConvertTo-Json -Depth 5
	Invoke-RestMethod -Method Put -Uri "$SupabaseUrl/auth/v1/admin/users/$($existing.id)" `
		-Headers $headers -ContentType "application/json" -Body $patchBody | Out-Null
	Write-Output "Patched app_metadata.role = admin on $Email"
}
