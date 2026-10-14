namespace MZikmund.Web.Configuration;

/// <summary>
/// Rate limits for the comment write endpoints. Bound from the optional [RateLimits] section.
/// </summary>
/// <remarks>
/// The defaults are the spec starting limits, so the section may be absent. Tune in production
/// with environment variables such as RateLimits__CommentCreate__PermitLimit.
/// </remarks>
public class RateLimitOptions
{
	/// <summary>
	/// Comment creates, per author. Default: 5 per 10 minutes.
	/// </summary>
	public RateLimitRule CommentCreate { get; set; } = new() { PermitLimit = 5, WindowMinutes = 10 };

	/// <summary>
	/// Votes, per author. Default: 30 per minute.
	/// </summary>
	public RateLimitRule CommentVote { get; set; } = new() { PermitLimit = 30, WindowMinutes = 1 };

	/// <summary>
	/// Reports, per author. Default: 10 per hour.
	/// </summary>
	public RateLimitRule CommentReport { get; set; } = new() { PermitLimit = 10, WindowMinutes = 60 };

	/// <summary>
	/// Every comment write request, per client IP, on top of the per-endpoint policies. Default: 20 per minute.
	/// </summary>
	public RateLimitRule GlobalWrites { get; set; } = new() { PermitLimit = 20, WindowMinutes = 1 };
}
