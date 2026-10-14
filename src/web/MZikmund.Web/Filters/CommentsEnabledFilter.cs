using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using MZikmund.Web.Configuration;

namespace MZikmund.Web.Filters;

/// <summary>
/// Returns 404 from the public comment endpoints while <c>Comments:Enabled</c> is false.
/// </summary>
/// <remarks>
/// Applied to <c>CommentsController</c> only. The admin controller stays reachable while the
/// feature is dark so moderation can be smoke-tested in production.
/// </remarks>
public sealed class CommentsEnabledFilter : IActionFilter
{
	private readonly CommentsOptions _options;

	public CommentsEnabledFilter(IOptions<CommentsOptions> options)
	{
		_options = options.Value;
	}

	public void OnActionExecuting(ActionExecutingContext context)
	{
		if (!_options.Enabled)
		{
			context.Result = new NotFoundResult();
		}
	}

	public void OnActionExecuted(ActionExecutedContext context)
	{
	}
}
