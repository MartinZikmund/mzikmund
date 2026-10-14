namespace MZikmund.Web.Configuration;

/// <summary>
/// One rate limiting rule: how many requests a partition may make inside one window.
/// </summary>
public class RateLimitRule
{
	/// <summary>
	/// Requests permitted inside a single window.
	/// </summary>
	public int PermitLimit { get; set; }

	/// <summary>
	/// Window length, in minutes.
	/// </summary>
	public int WindowMinutes { get; set; }

	/// <summary>
	/// Window length. Derived from <see cref="WindowMinutes"/>; get-only, so never bound directly.
	/// </summary>
	public TimeSpan Window => TimeSpan.FromMinutes(WindowMinutes);
}
