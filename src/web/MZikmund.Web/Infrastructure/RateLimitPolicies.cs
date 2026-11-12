using Microsoft.AspNetCore.RateLimiting;

namespace MZikmund.Web.Infrastructure;

/// <summary>
/// Rate limiting policy names, shared between the registration and the [EnableRateLimiting]
/// attributes on the comment controllers so the two cannot drift.
/// </summary>
public static class RateLimitPolicies
{
	/// <summary>Comment creates, per author.</summary>
	public const string CommentCreate = "comments-create";

	/// <summary>Votes, per author.</summary>
	public const string CommentVote = "comments-vote";

	/// <summary>Reports, per author.</summary>
	public const string CommentReport = "comments-report";

	/// <summary>Edits and deletes of own comments, per author.</summary>
	public const string CommentEdit = "comments-edit";

	/// <summary>
	/// True when the request is routed to an endpoint guarded by one of the comment write policies.
	/// </summary>
	public static bool IsCommentWrite(HttpContext httpContext)
	{
		var policy = httpContext.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName;
		return policy is CommentCreate or CommentVote or CommentReport or CommentEdit;
	}
}
