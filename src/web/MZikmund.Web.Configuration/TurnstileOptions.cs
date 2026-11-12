namespace MZikmund.Web.Configuration;

/// <summary>
/// Cloudflare Turnstile keys used by the web comment form.
/// Loaded from appsettings.json [Turnstile] section.
/// </summary>
public class TurnstileOptions
{
	/// <summary>
	/// Public site key rendered into the widget. Cloudflare's always-pass test key is
	/// 1x00000000000000000000AA and the always-fail one is 2x00000000000000000000AB.
	/// </summary>
	public string SiteKey { get; set; } = "";

	/// <summary>
	/// Server-side key used against the siteverify endpoint. Never committed for production -
	/// user secrets locally, App Service configuration in Azure.
	/// </summary>
	public string SecretKey { get; set; } = "";

	/// <summary>
	/// Value that must match the widget's data-action. Cloudflare echoes the action back but does
	/// not enforce it, so the verifier compares it. An empty value disables the check.
	/// </summary>
	public string ExpectedAction { get; set; } = "comment-create";

	/// <summary>
	/// Hostname the widget must have been solved on, for example mzikmund.dev. Cloudflare reports it
	/// but does not enforce it, so the verifier compares it. An empty value disables the check;
	/// Cloudflare's test keys report a placeholder hostname, so leave it empty in development.
	/// </summary>
	public string ExpectedHostname { get; set; } = "";
}
