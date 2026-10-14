using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using Moq;
using MZikmund.DataContracts;
using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Web.Controllers;
using MZikmund.Web.Core.Features.Comments;
using MZikmund.Web.Core.Services.Comments;
using MZikmund.Web.Filters;
using MZikmund.Web.Infrastructure;

namespace MZikmund.Web.Tests.Controllers;

public class CommentsControllerTests
{
	private static readonly Guid PostId = new("11111111-1111-1111-1111-111111111111");
	private static readonly Guid CommentId = new("22222222-2222-2222-2222-222222222222");

	private readonly Mock<IMediator> _mockMediator;
	private readonly Mock<ITurnstileVerifier> _mockTurnstileVerifier;
	private readonly Mock<ILogger<CommentsController>> _mockLogger;
	private readonly CommentsController _controller;

	public CommentsControllerTests()
	{
		_mockMediator = new Mock<IMediator>();
		_mockTurnstileVerifier = new Mock<ITurnstileVerifier>();
		_mockLogger = new Mock<ILogger<CommentsController>>();
		_controller = new CommentsController(
			_mockMediator.Object,
			_mockTurnstileVerifier.Object,
			_mockLogger.Object);
		_controller.ControllerContext = new ControllerContext
		{
			HttpContext = new DefaultHttpContext()
		};
	}

	[Fact]
	public async Task GetPostComments_WithDefaults_SendsTopSortAndPageSize20()
	{
		// Arrange
		var paged = new PagedResponse<CommentDto>(new[] { CreateDto() }, 1, 20, 1);
		_mockMediator
			.Setup(x => x.Send(It.IsAny<GetPostCommentsQuery>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(paged);

		// Act
		var result = await _controller.GetPostComments(PostId);

		// Assert
		var okResult = Assert.IsType<OkObjectResult>(result);
		Assert.Equal(200, okResult.StatusCode);
		_mockMediator.Verify(
			x => x.Send(
				It.Is<GetPostCommentsQuery>(q =>
					q.PostId == PostId &&
					q.Sort == CommentSortOrder.Top &&
					q.PageNumber == 1 &&
					q.PageSize == 20),
				It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task GetPostComments_WithNewSortAndPage3_ForwardsBoth()
	{
		// Arrange
		var paged = new PagedResponse<CommentDto>(Array.Empty<CommentDto>(), 3, 20, 0);
		_mockMediator
			.Setup(x => x.Send(It.IsAny<GetPostCommentsQuery>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(paged);

		// Act
		var result = await _controller.GetPostComments(PostId, CommentSortOrder.New, 3);

		// Assert
		Assert.IsType<OkObjectResult>(result);
		_mockMediator.Verify(
			x => x.Send(
				It.Is<GetPostCommentsQuery>(q => q.Sort == CommentSortOrder.New && q.PageNumber == 3),
				It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task GetCommentReplies_SendsQueryWithCommentId()
	{
		// Arrange
		var paged = new PagedResponse<CommentDto>(Array.Empty<CommentDto>(), 1, 20, 0);
		_mockMediator
			.Setup(x => x.Send(It.IsAny<GetCommentRepliesQuery>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(paged);

		// Act
		var result = await _controller.GetCommentReplies(CommentId);

		// Assert
		Assert.IsType<OkObjectResult>(result);
		_mockMediator.Verify(
			x => x.Send(
				It.Is<GetCommentRepliesQuery>(q => q.CommentId == CommentId && q.PageSize == 20),
				It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task CreateComment_WithoutTurnstileToken_IsAcceptedAndSkipsVerification()
	{
		// Arrange
		_mockMediator
			.Setup(x => x.Send(It.IsAny<CreateCommentCommand>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(CreateDto());
		var request = new CreateCommentRequest { Content = "Hello" };

		// Act
		var result = await _controller.CreateComment(PostId, request, null, CancellationToken.None);

		// Assert
		Assert.IsType<OkObjectResult>(result);
		_mockTurnstileVerifier.Verify(
			x => x.VerifyAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
			Times.Never);
		_mockMediator.Verify(
			x => x.Send(
				It.Is<CreateCommentCommand>(c => c.PostId == PostId && c.Content == "Hello"),
				It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task CreateComment_WithVerifyingTurnstileToken_SendsCommand()
	{
		// Arrange
		_mockTurnstileVerifier
			.Setup(x => x.VerifyAsync("good-token", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);
		_mockMediator
			.Setup(x => x.Send(It.IsAny<CreateCommentCommand>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(CreateDto());
		var request = new CreateCommentRequest { Content = "Hello" };

		// Act
		var result = await _controller.CreateComment(PostId, request, "good-token", CancellationToken.None);

		// Assert
		Assert.IsType<OkObjectResult>(result);
		_mockMediator.Verify(
			x => x.Send(It.IsAny<CreateCommentCommand>(), It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task CreateComment_WithFailingTurnstileToken_ReturnsBadRequestAndSendsNothing()
	{
		// Arrange
		_mockTurnstileVerifier
			.Setup(x => x.VerifyAsync("bad-token", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);
		var request = new CreateCommentRequest { Content = "Hello" };

		// Act
		var result = await _controller.CreateComment(PostId, request, "bad-token", CancellationToken.None);

		// Assert
		var badRequest = Assert.IsType<BadRequestObjectResult>(result);
		Assert.Equal(400, badRequest.StatusCode);
		_mockMediator.Verify(
			x => x.Send(It.IsAny<CreateCommentCommand>(), It.IsAny<CancellationToken>()),
			Times.Never);
	}

	[Fact]
	public async Task CreateComment_WithParentId_ForwardsParentId()
	{
		// Arrange
		var parentId = new Guid("33333333-3333-3333-3333-333333333333");
		_mockMediator
			.Setup(x => x.Send(It.IsAny<CreateCommentCommand>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(CreateDto());
		var request = new CreateCommentRequest { Content = "Reply", ParentId = parentId };

		// Act
		await _controller.CreateComment(PostId, request, null, CancellationToken.None);

		// Assert
		_mockMediator.Verify(
			x => x.Send(
				It.Is<CreateCommentCommand>(c => c.ParentId == parentId),
				It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task UpdateComment_SendsUpdateCommandWithContent()
	{
		// Arrange
		_mockMediator
			.Setup(x => x.Send(It.IsAny<UpdateCommentCommand>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(CreateDto());
		var request = new UpdateCommentRequest { Content = "Edited" };

		// Act
		var result = await _controller.UpdateComment(CommentId, request, CancellationToken.None);

		// Assert
		Assert.IsType<OkObjectResult>(result);
		_mockMediator.Verify(
			x => x.Send(
				It.Is<UpdateCommentCommand>(c => c.CommentId == CommentId && c.Content == "Edited"),
				It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task DeleteComment_ReturnsNoContent()
	{
		// Arrange
		_mockMediator
			.Setup(x => x.Send(It.IsAny<DeleteCommentCommand>(), It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);

		// Act
		var result = await _controller.DeleteComment(CommentId, CancellationToken.None);

		// Assert
		Assert.IsType<NoContentResult>(result);
		_mockMediator.Verify(
			x => x.Send(
				It.Is<DeleteCommentCommand>(c => c.CommentId == CommentId),
				It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task AddVote_SendsSetCommentVoteCommandWithUpvotedTrue()
	{
		// Arrange
		_mockMediator
			.Setup(x => x.Send(It.IsAny<SetCommentVoteCommand>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(CreateDto());

		// Act
		var result = await _controller.AddVote(CommentId, CancellationToken.None);

		// Assert
		Assert.IsType<OkObjectResult>(result);
		_mockMediator.Verify(
			x => x.Send(
				It.Is<SetCommentVoteCommand>(c => c.CommentId == CommentId && c.Upvoted),
				It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task RemoveVote_SendsSetCommentVoteCommandWithUpvotedFalse()
	{
		// Arrange
		_mockMediator
			.Setup(x => x.Send(It.IsAny<SetCommentVoteCommand>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(CreateDto());

		// Act
		var result = await _controller.RemoveVote(CommentId, CancellationToken.None);

		// Assert
		Assert.IsType<OkObjectResult>(result);
		_mockMediator.Verify(
			x => x.Send(
				It.Is<SetCommentVoteCommand>(c => c.CommentId == CommentId && !c.Upvoted),
				It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public async Task ReportComment_ReturnsNoContentAndForwardsReasonAndNote()
	{
		// Arrange
		_mockMediator
			.Setup(x => x.Send(It.IsAny<ReportCommentCommand>(), It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);
		var request = new ReportCommentRequest
		{
			Reason = CommentReportReason.Spam,
			Note = "Link farm"
		};

		// Act
		var result = await _controller.ReportComment(CommentId, request, CancellationToken.None);

		// Assert
		Assert.IsType<NoContentResult>(result);
		_mockMediator.Verify(
			x => x.Send(
				It.Is<ReportCommentCommand>(c =>
					c.CommentId == CommentId &&
					c.Reason == CommentReportReason.Spam &&
					c.Note == "Link farm"),
				It.IsAny<CancellationToken>()),
			Times.Once);
	}

	[Fact]
	public void Controller_HasClassLevelAuthorizeAndRouteApiV1()
	{
		// Arrange
		var type = typeof(CommentsController);

		// Act
		var authorize = type.GetCustomAttributes(typeof(AuthorizeAttribute), false);
		var route = type.GetCustomAttributes(typeof(RouteAttribute), false).Cast<RouteAttribute>().Single();

		// Assert
		Assert.Single(authorize);
		Assert.Equal("api/v1", route.Template);
	}

	[Theory]
	[InlineData(nameof(CommentsController.GetPostComments), true)]
	[InlineData(nameof(CommentsController.GetCommentReplies), true)]
	[InlineData(nameof(CommentsController.CreateComment), false)]
	[InlineData(nameof(CommentsController.UpdateComment), false)]
	[InlineData(nameof(CommentsController.DeleteComment), false)]
	[InlineData(nameof(CommentsController.AddVote), false)]
	[InlineData(nameof(CommentsController.RemoveVote), false)]
	[InlineData(nameof(CommentsController.ReportComment), false)]
	public void Actions_AllowAnonymousOnlyOnReads(string methodName, bool expectedAllowAnonymous)
	{
		// Arrange
		var method = typeof(CommentsController).GetMethod(methodName)!;

		// Act
		var allowAnonymous = method.GetCustomAttributes(typeof(AllowAnonymousAttribute), false);

		// Assert
		Assert.Equal(expectedAllowAnonymous, allowAnonymous.Length == 1);
	}

	[Theory]
	[InlineData(nameof(CommentsController.GetPostComments), typeof(HttpGetAttribute), "posts/{postId}/comments")]
	[InlineData(nameof(CommentsController.GetCommentReplies), typeof(HttpGetAttribute), "comments/{id}/replies")]
	[InlineData(nameof(CommentsController.CreateComment), typeof(HttpPostAttribute), "posts/{postId}/comments")]
	[InlineData(nameof(CommentsController.UpdateComment), typeof(HttpPatchAttribute), "comments/{id}")]
	[InlineData(nameof(CommentsController.DeleteComment), typeof(HttpDeleteAttribute), "comments/{id}")]
	[InlineData(nameof(CommentsController.AddVote), typeof(HttpPutAttribute), "comments/{id}/vote")]
	[InlineData(nameof(CommentsController.RemoveVote), typeof(HttpDeleteAttribute), "comments/{id}/vote")]
	[InlineData(nameof(CommentsController.ReportComment), typeof(HttpPostAttribute), "comments/{id}/report")]
	public void Actions_HaveTheContractRouteTemplates(string methodName, Type verbAttribute, string template)
	{
		// Arrange
		var method = typeof(CommentsController).GetMethod(methodName)!;

		// Act
		var attribute = method.GetCustomAttributes(verbAttribute, false)
			.Cast<IRouteTemplateProvider>()
			.Single();

		// Assert
		Assert.Equal(template, attribute.Template);
	}

	[Fact]
	public void Controller_UsesTheEnabledGateAndTheExceptionFilter()
	{
		// Arrange
		var type = typeof(CommentsController);

		// Act
		var gate = type.GetCustomAttributes(typeof(ServiceFilterAttribute), false).Cast<ServiceFilterAttribute>().Single();
		var exceptionFilter = type.GetCustomAttributes(typeof(CommentsExceptionFilterAttribute), false);

		// Assert
		Assert.Equal(typeof(CommentsEnabledFilter), gate.ServiceType);
		Assert.Single(exceptionFilter);
	}

	[Theory]
	[InlineData(nameof(CommentsController.CreateComment), RateLimitPolicies.CommentCreate)]
	[InlineData(nameof(CommentsController.AddVote), RateLimitPolicies.CommentVote)]
	[InlineData(nameof(CommentsController.RemoveVote), RateLimitPolicies.CommentVote)]
	[InlineData(nameof(CommentsController.ReportComment), RateLimitPolicies.CommentReport)]
	public void WriteActions_AreRateLimitedByTheirPolicy(string methodName, string policy)
	{
		// Arrange
		var method = typeof(CommentsController).GetMethod(methodName)!;

		// Act
		var attribute = method.GetCustomAttributes(typeof(EnableRateLimitingAttribute), false)
			.Cast<EnableRateLimitingAttribute>()
			.Single();

		// Assert
		Assert.Equal(policy, attribute.PolicyName);
	}

	private static CommentDto CreateDto() => new()
	{
		Id = CommentId,
		PostId = PostId,
		AuthorDisplayName = "Martin",
		Content = "Hello",
		ContentHtml = "<p>Hello</p>",
		Status = CommentStatus.Visible,
		CreatedOn = new DateTime(2026, 8, 11, 12, 0, 0, DateTimeKind.Utc),
		UpvoteCount = 0,
		ReplyCount = 0,
		HasUpvoted = false,
		IsOwn = false
	};
}
