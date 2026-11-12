using System.Threading.RateLimiting;
using MZikmund.Web.Configuration;

namespace MZikmund.Web.Infrastructure;

/// <summary>
/// Extra per-user create limit for Uno app requests that carry no Turnstile token.
/// Acquired manually from the controller, since it only applies to some requests.
/// </summary>
public sealed class AppCommentCreateLimiter : IDisposable
{
	private readonly PartitionedRateLimiter<string> _limiter;

	public AppCommentCreateLimiter(RateLimitRule rule) =>
		_limiter = PartitionedRateLimiter.Create<string, string>(key =>
			RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
			{
				PermitLimit = rule.PermitLimit,
				Window = rule.Window,
				QueueLimit = 0,
				AutoReplenishment = true,
			}));

	public RateLimitLease Acquire(HttpContext httpContext) =>
		_limiter.AttemptAcquire(RateLimitPartitionKeys.ForRequest(httpContext));

	public void Dispose() => _limiter.Dispose();
}
