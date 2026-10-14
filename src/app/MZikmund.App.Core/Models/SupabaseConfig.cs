namespace MZikmund.Services.Account;

public record SupabaseConfig
{
	/// <summary>
	/// Base URL of the Supabase project, e.g. https://abcd.supabase.co or http://127.0.0.1:54321.
	/// </summary>
	public string? Url { get; init; }

	/// <summary>
	/// Publishable (sb_publishable_...) key sent as the apikey header. Public by design.
	/// </summary>
	public string? PublishableKey { get; init; }

	/// <summary>
	/// Loopback port for the desktop sign-in redirect. Supabase must allow http://127.0.0.1:{port}/callback.
	/// </summary>
	public int DesktopCallbackPort { get; init; } = 51842;

	public bool IsConfigured =>
		Uri.TryCreate(Url, UriKind.Absolute, out _) &&
		!string.IsNullOrWhiteSpace(PublishableKey) &&
		!PublishableKey.Contains("Placeholder", StringComparison.OrdinalIgnoreCase);
}
