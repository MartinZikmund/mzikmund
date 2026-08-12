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
}
