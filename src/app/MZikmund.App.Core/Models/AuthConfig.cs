namespace MZikmund.Services.Account;

public record AuthConfig
{
	public string? ClientId { get; init; }

	public string Domain { get; init; } = "mzikmunddev.eu.auth0.com";

	public string Audience { get; init; } = "https://mzikmund.dev/api";

	/// <summary>
	/// Loopback port for the desktop sign-in redirect. Auth0 must allow http://127.0.0.1:{port}/callback.
	/// </summary>
	public int DesktopCallbackPort { get; init; } = 51842;

	public bool IsConfigured =>
		!string.IsNullOrWhiteSpace(ClientId) &&
		!ClientId.Contains("your-auth0-client-id", StringComparison.OrdinalIgnoreCase) &&
		!ClientId.Contains("Placeholder", StringComparison.OrdinalIgnoreCase);
}
