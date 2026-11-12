using System.Security.Claims;

namespace MZikmund.Web.Infrastructure;

/// <summary>
/// Builds rate limiting partition keys for the comment endpoints.
/// </summary>
public static class RateLimitPartitionKeys
{
	/// <summary>
	/// Authenticated user id when present, client IP otherwise.
	/// </summary>
	/// <remarks>
	/// The identity comes from the Supabase <c>sub</c> claim, not <c>User.Identity.Name</c>: a
	/// Supabase token carries no name claim, so every user would share one bucket. Keys are
	/// prefixed by kind so a crafted user id cannot collide with an IP.
	/// </remarks>
	public static string ForRequest(HttpContext httpContext)
	{
		var subject = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
			?? httpContext.User.FindFirst("sub")?.Value;

		return string.IsNullOrEmpty(subject)
			? ForClientIp(httpContext)
			: $"user:{subject}";
	}

	/// <summary>
	/// Client-IP partition key.
	/// </summary>
	public static string ForClientIp(HttpContext httpContext) =>
		$"ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
}
