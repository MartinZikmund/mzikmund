namespace MZikmund.Web.Infrastructure.Auth;

/// <summary>
/// Claim types and well-known claim values used with Supabase-issued access tokens.
/// </summary>
/// <remarks>
/// Projected claim types are namespaced with a "supabase:" prefix so they can never collide with the
/// flat "role" claim Supabase already emits, which carries the Postgres role ("authenticated"),
/// not an application role.
/// </remarks>
public static class SupabaseClaims
{
	/// <summary>Flat subject claim carrying the Supabase user id (a GUID).</summary>
	public const string Subject = "sub";

	/// <summary>Flat email claim.</summary>
	public const string Email = "email";

	/// <summary>The only audience Supabase issues access tokens for.</summary>
	public const string Audience = "authenticated";

	/// <summary>Server-controlled metadata. Value is a raw JSON object, not a flat string.</summary>
	public const string AppMetadata = "app_metadata";

	/// <summary>User-editable metadata. Value is a raw JSON object, not a flat string.</summary>
	public const string UserMetadata = "user_metadata";

	/// <summary>Projected from app_metadata.role. Safe to authorize on.</summary>
	public const string Role = "supabase:role";

	/// <summary>Projected from user_metadata.full_name or user_metadata.name.</summary>
	public const string DisplayName = "supabase:display_name";

	/// <summary>Projected from user_metadata.avatar_url or user_metadata.picture.</summary>
	public const string AvatarUrl = "supabase:avatar_url";

	/// <summary>Marker claim proving the projection already ran for this principal.</summary>
	public const string Projected = "supabase:projected";

	/// <summary>The app_metadata.role value that grants access to the admin surface.</summary>
	public const string AdminRole = "admin";
}
