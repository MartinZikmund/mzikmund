using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Web.Core.Services.Comments;

namespace MZikmund.Web.Filters;

/// <summary>
/// Turns the domain exceptions thrown by the comment handlers into ProblemDetails responses.
/// The exception text can carry identifiers, so the client gets a fixed message and a stable
/// <c>code</c> per failure; the full text only goes to the log.
/// Anything else is a bug and is left alone, so it surfaces as a 500 without leaking its message.
/// Scoped to the comment controllers so existing controllers keep their behavior.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class CommentsExceptionFilterAttribute : ExceptionFilterAttribute
{
	public override void OnException(ExceptionContext context)
	{
		(int Status, string Code)? mapped = context.Exception switch
		{
			CommentNotFoundException => (StatusCodes.Status404NotFound, CommentErrorCodes.NotFound),
			CommentForbiddenException => (StatusCodes.Status403Forbidden, CommentErrorCodes.Forbidden),
			CommentValidationException => (StatusCodes.Status400BadRequest, CommentErrorCodes.Validation),
			CommentConflictException => (StatusCodes.Status409Conflict, CommentErrorCodes.Conflict),
			_ => null,
		};

		if (mapped is not { } result)
		{
			return;
		}

		var status = result.Status;
		var code = (context.Exception as CommentException)?.Code ?? result.Code;
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
			Detail = PublicMessage(code),
		};
		problem.Extensions["code"] = code;

		context.Result = new ObjectResult(problem)
		{
			StatusCode = status,
			ContentTypes = { "application/problem+json" },
		};
		context.ExceptionHandled = true;
	}

	public static string PublicMessage(string code) => code switch
	{
		CommentErrorCodes.NotFound => "That comment or post is no longer available.",
		CommentErrorCodes.Forbidden => "You are not allowed to do that.",
		CommentErrorCodes.Validation => "The request is not valid.",
		CommentErrorCodes.Conflict => "That action is not available for this comment.",
		CommentErrorCodes.Banned => "Your account is not allowed to comment.",
		CommentErrorCodes.ContentEmpty => "A comment cannot be empty.",
		CommentErrorCodes.ContentTooLong => $"A comment cannot be longer than {CommentLimits.MaxCommentLength} characters.",
		CommentErrorCodes.EditWindowExpired => $"A comment can only be edited within {CommentLimits.EditWindowMinutes} minutes of posting.",
		CommentErrorCodes.NotVisible => "That comment is no longer visible.",
		CommentErrorCodes.OwnComment => "You cannot do that to your own comment.",
		CommentErrorCodes.HiddenByModerator => "A moderator hid this comment, so it cannot be deleted.",
		_ => "The request could not be completed.",
	};
}
