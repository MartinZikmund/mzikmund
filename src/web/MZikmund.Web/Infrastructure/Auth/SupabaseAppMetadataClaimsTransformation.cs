using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace MZikmund.Web.Infrastructure.Auth;

/// <summary>
/// Projects members of Supabase's nested app_metadata / user_metadata objects into first-class claims.
/// </summary>
/// <remarks>
/// Microsoft.IdentityModel surfaces a nested JSON object as a SINGLE claim whose value is the entire
/// serialized object, so "app_metadata.role" never exists as its own claim and RequireClaim can never
/// reach it. Without this transformation AdminPolicy can never succeed.
/// </remarks>
public sealed class SupabaseAppMetadataClaimsTransformation : IClaimsTransformation
{
	/// <inheritdoc />
	public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
	{
		if (principal.Identity is not ClaimsIdentity { IsAuthenticated: true })
		{
			return Task.FromResult(principal);
		}

		// TransformAsync can run more than once per request, so stay idempotent.
		if (principal.HasClaim(claim => claim.Type == SupabaseClaims.Projected))
		{
			return Task.FromResult(principal);
		}

		ClaimsIdentity projected = new();
		projected.AddClaim(new Claim(SupabaseClaims.Projected, "true"));

		if (TryReadMetadataString(principal, SupabaseClaims.AppMetadata, "role", out var role))
		{
			projected.AddClaim(new Claim(SupabaseClaims.Role, role));
		}

		if (TryReadMetadataString(principal, SupabaseClaims.UserMetadata, "full_name", out var displayName) ||
			TryReadMetadataString(principal, SupabaseClaims.UserMetadata, "name", out displayName))
		{
			projected.AddClaim(new Claim(SupabaseClaims.DisplayName, displayName));
		}

		if (TryReadMetadataString(principal, SupabaseClaims.UserMetadata, "avatar_url", out var avatarUrl) ||
			TryReadMetadataString(principal, SupabaseClaims.UserMetadata, "picture", out avatarUrl))
		{
			projected.AddClaim(new Claim(SupabaseClaims.AvatarUrl, avatarUrl));
		}

		ClaimsPrincipal clone = new(principal.Identities.Select(identity => identity.Clone()));
		clone.AddIdentity(projected);
		return Task.FromResult(clone);
	}

	private static bool TryReadMetadataString(ClaimsPrincipal principal, string metadataClaimType, string propertyName, out string value)
	{
		value = string.Empty;

		var claim = principal.FindFirst(metadataClaimType);
		if (claim is null || string.IsNullOrEmpty(claim.Value))
		{
			return false;
		}

		try
		{
			using var document = JsonDocument.Parse(claim.Value);
			if (document.RootElement.ValueKind == JsonValueKind.Object &&
				document.RootElement.TryGetProperty(propertyName, out var element) &&
				element.ValueKind == JsonValueKind.String)
			{
				value = element.GetString() ?? string.Empty;
				return value.Length > 0;
			}
		}
		catch (JsonException)
		{
			// Malformed metadata must never fail the request; it simply yields no projected claim.
		}

		return false;
	}
}
