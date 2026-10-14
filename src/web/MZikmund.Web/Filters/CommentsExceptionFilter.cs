using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MZikmund.Web.Filters;

/// <summary>
/// Turns the BCL exceptions thrown by the comment handlers into ProblemDetails responses.
/// Scoped to the comment controllers so existing controllers keep their behavior.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class CommentsExceptionFilterAttribute : ExceptionFilterAttribute
{
	public override void OnException(ExceptionContext context)
	{
		int? mapped = context.Exception switch
		{
			KeyNotFoundException => StatusCodes.Status404NotFound,
			UnauthorizedAccessException => StatusCodes.Status403Forbidden,
			ArgumentException => StatusCodes.Status400BadRequest,
			InvalidOperationException => StatusCodes.Status409Conflict,
			_ => (int?)null,
		};

		if (mapped is not { } status)
		{
			return;
		}

		var message = context.Exception.Message;
		var logger = context.HttpContext.RequestServices.GetService<ILoggerFactory>()?
			.CreateLogger<CommentsExceptionFilterAttribute>();
		if (logger is not null && logger.IsEnabled(LogLevel.Information))
		{
			logger.LogInformation("Comment request rejected with {StatusCode}: {Message}", status, message);
		}

		ProblemDetails problem = new()
		{
			Status = status,
			Detail = message,
		};

		context.Result = new ObjectResult(problem)
		{
			StatusCode = status,
			ContentTypes = { "application/problem+json" },
		};
		context.ExceptionHandled = true;
	}
}
