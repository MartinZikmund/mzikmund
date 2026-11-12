using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using MZikmund.Web.Configuration;
using MZikmund.Web.Filters;

namespace MZikmund.Web.Tests.Filters;

public class CommentsEnabledFilterTests
{
	[Fact]
	public void OnActionExecuting_WhenCommentsDisabled_ShortCircuitsWithNotFound()
	{
		// Arrange
		CommentsEnabledFilter filter = new(Options.Create(new CommentsOptions { Enabled = false }));
		var context = CreateContext();

		// Act
		filter.OnActionExecuting(context);

		// Assert
		Assert.IsType<NotFoundResult>(context.Result);
	}

	[Fact]
	public void OnActionExecuting_WhenCommentsEnabled_LeavesResultUntouched()
	{
		// Arrange
		CommentsEnabledFilter filter = new(Options.Create(new CommentsOptions { Enabled = true }));
		var context = CreateContext();

		// Act
		filter.OnActionExecuting(context);

		// Assert
		Assert.Null(context.Result);
	}

	private static ActionExecutingContext CreateContext()
	{
		ActionContext actionContext = new(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());

		return new ActionExecutingContext(
			actionContext,
			new List<IFilterMetadata>(),
			new Dictionary<string, object?>(),
			controller: new object());
	}
}
