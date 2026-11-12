namespace MZikmund.Web.Configuration;

/// <summary>
/// Supabase identity provider settings.
/// Loaded from appsettings.json [Supabase] section.
/// </summary>
public class SupabaseOptions
{
	/// <summary>
	/// Hosted project reference, e.g. "abcdefghijklmnop". Every hosted URL is derived from it.
	/// </summary>
	public string ProjectRef { get; set; } = "";

	/// <summary>
	/// Publishable API key (sb_publishable_...). Public by design; sent as the "apikey" header.
	/// </summary>
	public string PublishableKey { get; set; } = "";

	/// <summary>
	/// Development-only override pointing at the local Supabase CLI stack, e.g.
	/// http://127.0.0.1:54321. A project ref cannot address a loopback stack, so this is the one
	/// piece of the configuration that differs between environments. Leave it unset in production.
	/// </summary>
	public string? LocalUrl { get; set; }

	/// <summary>
	/// Base URL of the Supabase instance, without a trailing slash.
	/// </summary>
	public string BaseUrl => string.IsNullOrWhiteSpace(LocalUrl)
		? $"https://{ProjectRef}.supabase.co"
		: LocalUrl.TrimEnd('/');

	/// <summary>
	/// The value GoTrue stamps into the "iss" claim of every access token.
	/// </summary>
	public string Issuer => $"{BaseUrl}/auth/v1";

	/// <summary>
	/// JWKS document used to validate access-token signatures. Served identically by the local
	/// CLI stack and by a hosted project.
	/// </summary>
	public string JwksUri => $"{Issuer}/.well-known/jwks.json";
}
