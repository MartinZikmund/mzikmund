using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Web.Core.Services.Comments;
using MZikmund.Web.Filters;

namespace MZikmund.Web.Tests.Filters;

public class CommentsExceptionFilterTests
{
	public static TheoryData<Exception, int> Mappings => new()
	{
		{ new CommentNotFoundException("missing"), 404 },
		{ new CommentForbiddenException("banned"), 403 },
		{ new CommentValidationException("empty"), 400 },
		{ new CommentConflictException("conflict"), 409 },
	};

	[Theory]
	[MemberData(nameof(Mappings))]
	public void OnException_MapsKnownExceptionsToProblemDetails(Exception exception, int expectedStatus)
	{
		// Arrange
		var filter = new CommentsExceptionFilterAttribute();
		var context = CreateContext(exception);

		// Act
		filter.OnException(context);

		// Assert
		Assert.True(context.ExceptionHandled);
		var result = Assert.IsType<ObjectResult>(context.Result);
		Assert.Equal(expectedStatus, result.StatusCode);
		var problem = Assert.IsType<ProblemDetails>(result.Value);
		Assert.Equal(expectedStatus, problem.Status);
		Assert.Equal(CommentsExceptionFilterAttribute.PublicMessage((string)problem.Extensions["code"]!), problem.Detail);
		Assert.DoesNotContain(exception.Message, problem.Detail);
	}

	[Fact]
	public void OnException_NeverLeaksIdentifiersFromTheExceptionMessage()
	{
		// Arrange
		var filter = new CommentsExceptionFilterAttribute();
		var id = Guid.NewGuid();
		var context = CreateContext(new CommentConflictException(
			$"Comment {id} can only be edited within 15 minutes of posting.",
			CommentErrorCodes.EditWindowExpired));

		// Act
		filter.OnException(context);

		// Assert
		var problem = Assert.IsType<ProblemDetails>(Assert.IsType<ObjectResult>(context.Result).Value);
		Assert.DoesNotContain(id.ToString(), problem.Detail);
		Assert.Equal(CommentErrorCodes.EditWindowExpired, problem.Extensions["code"]);
		Assert.Contains("15 minutes", problem.Detail);
	}

	public static TheoryData<Exception> Unmapped => new()
	{
		new NotSupportedException("boom"),
		new InvalidOperationException("sequence contains no elements"),
		new ArgumentException("bad argument"),
		new KeyNotFoundException("dictionary key"),
		new UnauthorizedAccessException("file system"),
	};

	[Theory]
	[MemberData(nameof(Unmapped))]
	public void OnException_NonDomainException_StaysUnhandledSoItSurfacesAs500(Exception exception)
	{
		// Arrange
		var filter = new CommentsExceptionFilterAttribute();
		var context = CreateContext(exception);

		// Act
		filter.OnException(context);

		// Assert
		Assert.False(context.ExceptionHandled);
		Assert.Null(context.Result);
	}

	private static ExceptionContext CreateContext(Exception exception)
	{
		DefaultHttpContext httpContext = new() { RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider() };
		ActionContext actionContext = new(httpContext, new RouteData(), new ActionDescriptor());
		return new ExceptionContext(actionContext, new List<IFilterMetadata>()) { Exception = exception };
	}
}
