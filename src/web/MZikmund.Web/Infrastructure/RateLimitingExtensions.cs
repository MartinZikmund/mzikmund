using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using MZikmund.Web.Configuration;

namespace MZikmund.Web.Infrastructure;

public static class RateLimitingExtensions
{
	/// <summary>
	/// Registers the comment write policies plus a per-IP limiter that only applies to comment
	/// writes, so admin uploads and post saves are never throttled.
	/// </summary>
	public static IServiceCollection AddCommentRateLimiting(this IServiceCollection services, RateLimitOptions limits) =>
		services.AddRateLimiter(options =>
		{
			// The framework default is 503; the comment API contract is 429.
			options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

			options.OnRejected = (context, _) =>
			{
				if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
				{
					context.HttpContext.Response.Headers.RetryAfter =
						((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);
				}

				context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

				var logger = context.HttpContext.RequestServices.GetService<ILoggerFactory>()?
					.CreateLogger("MZikmund.RateLimiting");
				if (logger is not null && logger.IsEnabled(LogLevel.Warning))
				{
					var partition = RateLimitPartitionKeys.ForRequest(context.HttpContext);
					var path = context.HttpContext.Request.Path.Value;
					logger.LogWarning("Rate limit hit for {Partition} on {Path}", partition, path);
				}

				return ValueTask.CompletedTask;
			};

			options.AddPolicy(RateLimitPolicies.CommentCreate, httpContext =>
				RateLimitPartition.GetFixedWindowLimiter(
					RateLimitPartitionKeys.ForRequest(httpContext),
					_ => CreateFixedWindow(limits.CommentCreate)));

			// Sliding so a window boundary cannot be gamed.
			options.AddPolicy(RateLimitPolicies.CommentVote, httpContext =>
				RateLimitPartition.GetSlidingWindowLimiter(
					RateLimitPartitionKeys.ForRequest(httpContext),
					_ => new SlidingWindowRateLimiterOptions
					{
						PermitLimit = limits.CommentVote.PermitLimit,
						Window = limits.CommentVote.Window,
						SegmentsPerWindow = 6,
						QueueLimit = 0,
						AutoReplenishment = true,
					}));

			options.AddPolicy(RateLimitPolicies.CommentReport, httpContext =>
				RateLimitPartition.GetFixedWindowLimiter(
					RateLimitPartitionKeys.ForRequest(httpContext),
					_ => CreateFixedWindow(limits.CommentReport)));

			options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
				RateLimitPolicies.IsCommentWrite(httpContext)
					? RateLimitPartition.GetFixedWindowLimiter(
						RateLimitPartitionKeys.ForClientIp(httpContext),
						_ => CreateFixedWindow(limits.GlobalWrites))
					: RateLimitPartition.GetNoLimiter("not-a-comment-write"));
		});

	// QueueLimit 0: reject immediately rather than stalling an interactive request.
	private static FixedWindowRateLimiterOptions CreateFixedWindow(RateLimitRule rule) => new()
	{
		PermitLimit = rule.PermitLimit,
		Window = rule.Window,
		QueueLimit = 0,
		AutoReplenishment = true,
	};
}
