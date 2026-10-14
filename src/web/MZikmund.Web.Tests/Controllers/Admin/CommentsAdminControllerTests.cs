using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Moq;
using MZikmund.DataContracts;
using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Web.Controllers.Admin;
using MZikmund.Web.Core.Features.Comments;
using MZikmund.Web.Filters;

namespace MZikmund.Web.Tests.Controllers.Admin;

public class CommentsAdminControllerTests
{
	private static readonly Guid CommentId = new("22222222-2222-2222-2222-222222222222");
	private static readonly Guid AuthorId = new("44444444-4444-4444-4444-444444444444");

	private readonly Mock<IMediator> _mockMediator;
	private readonly CommentsAdminController _controller;

	public CommentsAdminControllerTests()
	{
		_mockMediator = new Mock<IMediator>();
		_controller = new CommentsAdminController(_mockMediator.Object);
	}

	[Fact]
	public async Task GetReports_SendsQueryWithPageSize20()
	{
		// Arrange
		var paged = new PagedResponse<CommentReportDto>(Array.Empty<CommentReportDto>(), 1, 20, 0);
		_mockMediator
			.Setup(x => x.Send(It.IsAny<GetCommentReportsQuery>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(paged);

		// Act
		var result = await _controller.GetReports();

		// Assert
		var okResult = Assert.IsType<OkObjectResult>(result);
		Assert.Equal(200, okResult.StatusCode);
		_mockMediator.Verify(
			x => x.Send(
				It.Is<GetCommentReportsQuery>(q => q.PageNumber == 1 && q.PageSize == 20),
				It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task ResolveReport_ReturnsNoContent()
	{
		// Arrange
		_mockMediator
			.Setup(x => x.Send(It.IsAny<ResolveCommentReportCommand>(), It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);

		// Act
		var result = await _controller.ResolveReport(CommentId, CancellationToken.None);

		// Assert
		Assert.IsType<NoContentResult>(result);
		_mockMediator.Verify(
			x => x.Send(
				It.Is<ResolveCommentReportCommand>(c => c.CommentId == CommentId),
				It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task GetModerationFeed_ForwardsSinceAndPageNumber()
	{
		// Arrange
		var since = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
		var paged = new PagedResponse<CommentDto>(Array.Empty<CommentDto>(), 2, 20, 0);
		_mockMediator
			.Setup(x => x.Send(It.IsAny<GetModerationFeedQuery>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(paged);

		// Act
		var result = await _controller.GetModerationFeed(since, 2);

		// Assert
		Assert.IsType<OkObjectResult>(result);
		_mockMediator.Verify(
			x => x.Send(
				It.Is<GetModerationFeedQuery>(q => q.Since == since && q.PageNumber == 2 && q.PageSize == 20),
				It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task CountNewComments_ReturnsOkWithCount()
	{
		// Arrange
		var since = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
		_mockMediator
			.Setup(x => x.Send(It.IsAny<CountNewCommentsQuery>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(7);

		// Act
		var result = await _controller.CountNewComments(since);

		// Assert
		var okResult = Assert.IsType<OkObjectResult>(result);
		Assert.Equal(7, okResult.Value);
		_mockMediator.Verify(
			x => x.Send(
				It.Is<CountNewCommentsQuery>(q => q.Since == since),
				It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task HideComment_ReturnsNoContent()
	{
		// Arrange
		_mockMediator
			.Setup(x => x.Send(It.IsAny<HideCommentCommand>(), It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);

		// Act
		var result = await _controller.HideComment(CommentId, CancellationToken.None);

		// Assert
		Assert.IsType<NoContentResult>(result);
		_mockMediator.Verify(
			x => x.Send(
				It.Is<HideCommentCommand>(c => c.CommentId == CommentId),
				It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task DeleteComment_SendsAdminDeleteAndReturnsNoContent()
	{
		// Arrange
		_mockMediator
			.Setup(x => x.Send(It.IsAny<AdminDeleteCommentCommand>(), It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);

		// Act
		var result = await _controller.DeleteComment(CommentId, CancellationToken.None);

		// Assert
		Assert.IsType<NoContentResult>(result);
		_mockMediator.Verify(
			x => x.Send(
				It.Is<AdminDeleteCommentCommand>(c => c.CommentId == CommentId),
				It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task BanAuthor_ReturnsNoContent()
	{
		// Arrange
		_mockMediator
			.Setup(x => x.Send(It.IsAny<BanAuthorCommand>(), It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);

		// Act
		var result = await _controller.BanAuthor(AuthorId, CancellationToken.None);

		// Assert
		Assert.IsType<NoContentResult>(result);
		_mockMediator.Verify(
			x => x.Send(
				It.Is<BanAuthorCommand>(c => c.AuthorId == AuthorId),
				It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public void Controller_HasAdminPolicyAndAdminRoutePrefix()
	{
		// Arrange
		var type = typeof(CommentsAdminController);

		// Act
		var authorize = type.GetCustomAttributes(typeof(AuthorizeAttribute), false)
			.Cast<AuthorizeAttribute>()
			.Single();
		var route = type.GetCustomAttributes(typeof(RouteAttribute), false)
			.Cast<RouteAttribute>()
			.Single();

		// Assert
		Assert.Equal("AdminPolicy", authorize.Policy);
		Assert.Equal("api/v1/admin/comments", route.Template);
	}

	[Fact]
	public async Task GetModerationFeed_WithLocalSince_SendsUtc()
	{
		// Arrange
		var since = new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Local);
		_mockMediator
			.Setup(x => x.Send(It.IsAny<GetModerationFeedQuery>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new PagedResponse<CommentDto>(Array.Empty<CommentDto>(), 1, 20, 0));

		// Act
		await _controller.GetModerationFeed(since);

		// Assert
		_mockMediator.Verify(
			x => x.Send(
				It.Is<GetModerationFeedQuery>(q => q.Since == since.ToUniversalTime() && q.Since!.Value.Kind == DateTimeKind.Utc),
				It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task CountNewComments_WithoutSince_SendsNull()
	{
		// Arrange
		_mockMediator
			.Setup(x => x.Send(It.IsAny<CountNewCommentsQuery>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(0);

		// Act
		await _controller.CountNewComments();

		// Assert
		_mockMediator.Verify(
			x => x.Send(It.Is<CountNewCommentsQuery>(q => q.Since == null), It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public void Controller_UsesTheCommentsExceptionFilter()
	{
		// Arrange
		var type = typeof(CommentsAdminController);

		// Act
		var filters = type.GetCustomAttributes(typeof(CommentsExceptionFilterAttribute), false);

		// Assert
		Assert.Single(filters);
	}

	[Fact]
	public void Controller_IsNotGatedByTheCommentsEnabledFilter()
	{
		// Arrange
		var type = typeof(CommentsAdminController);

		// Act
		var serviceFilters = type.GetCustomAttributes(typeof(ServiceFilterAttribute), false);

		// Assert - moderation must stay reachable while the feature is dark
		Assert.Empty(serviceFilters);
	}

	[Theory]
	[InlineData(nameof(CommentsAdminController.GetReports), typeof(HttpGetAttribute), "reports")]
	[InlineData(nameof(CommentsAdminController.ResolveReport), typeof(HttpPostAttribute), "reports/{commentId}/resolve")]
	[InlineData(nameof(CommentsAdminController.GetModerationFeed), typeof(HttpGetAttribute), "")]
	[InlineData(nameof(CommentsAdminController.CountNewComments), typeof(HttpGetAttribute), "count")]
	[InlineData(nameof(CommentsAdminController.HideComment), typeof(HttpPostAttribute), "{id}/hide")]
	[InlineData(nameof(CommentsAdminController.DeleteComment), typeof(HttpDeleteAttribute), "{id}")]
	[InlineData(nameof(CommentsAdminController.BanAuthor), typeof(HttpPostAttribute), "~/api/v1/admin/authors/{id}/ban")]
	public void Actions_HaveTheContractRouteTemplates(string methodName, Type verbAttribute, string template)
	{
		// Arrange
		var method = typeof(CommentsAdminController).GetMethod(methodName)!;

		// Act
		var attribute = method.GetCustomAttributes(verbAttribute, false)
			.Cast<IRouteTemplateProvider>()
			.Single();

		// Assert
		Assert.Equal(template, attribute.Template);
	}
}
