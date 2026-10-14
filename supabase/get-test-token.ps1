#requires -Version 7
<#
.SYNOPSIS
	Signs in to the LOCAL Supabase stack headlessly and writes the access token to stdout.
.DESCRIPTION
	Email OTP is the only sign-in path that can be automated against a local stack. Google needs a
	real consent screen and actively blocks automated browsers; Apple rejects localhost and IP
	addresses as Service ID return URLs outright.

	The code is read out of Mailpit, which is why supabase/templates/*.html must contain
	{{ .Token }} - with the stock templates GoTrue mails a clickable link and no code exists.
#>
[CmdletBinding()]
param(
	[string]$Email = "admin@mzikmund.test",
	[string]$SupabaseUrl = "http://127.0.0.1:54321",
	[string]$MailpitUrl = "http://127.0.0.1:54324",
	[string]$PublishableKey = "sb_publishable_ACJWlzQHlZjBrEguHvfOxg_3BJgxAaH",
	[int]$TimeoutSeconds = 20
)

$ErrorActionPreference = "Stop"

# Wipe the mailbox so the next message is unambiguously ours.
Invoke-RestMethod -Method Delete -Uri "$MailpitUrl/api/v1/messages" `
	-ContentType "application/json" -Body "{}" | Out-Null

$otpHeaders = @{ apikey = $PublishableKey }
$otpBody = @{ email = $Email; create_user = $false } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri "$SupabaseUrl/auth/v1/otp" `
	-Headers $otpHeaders -ContentType "application/json" -Body $otpBody | Out-Null

$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
$code = $null
while ($null -eq $code -and (Get-Date) -lt $deadline)
{
	$query = [uri]::EscapeDataString("to:$Email")
	$search = Invoke-RestMethod -Method Get -Uri "$MailpitUrl/api/v1/search?query=$query&limit=1"
	if ($search.messages.Count -gt 0)
	{
		$message = Invoke-RestMethod -Method Get -Uri "$MailpitUrl/api/v1/message/$($search.messages[0].ID)"
		$body = "$($message.Text)`n$($message.HTML)"
		$match = [regex]::Match($body, '\b(\d{6})\b')
		if ($match.Success)
		{
			$code = $match.Groups[1].Value
		}
	}

	if ($null -eq $code)
	{
		Start-Sleep -Milliseconds 250
	}
}

if ($null -eq $code)
{
	throw "No OTP e-mail for $Email within $TimeoutSeconds seconds. Check that supabase/templates/*.html contain {{ .Token }}."
}

# type "email" is what a 6-digit email OTP requires. The published openapi.yaml omits it from the
# enum, but the server handles it; "magiclink" is the wrong type here and returns 400.
$verifyBody = @{ type = "email"; email = $Email; token = $code } | ConvertTo-Json
$session = Invoke-RestMethod -Method Post -Uri "$SupabaseUrl/auth/v1/verify" `
	-Headers $otpHeaders -ContentType "application/json" -Body $verifyBody

Write-Output $session.access_token
