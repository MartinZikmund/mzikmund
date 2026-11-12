using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MZikmund.Api.Client;
using MZikmund.DataContracts;
using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Models.Dialogs;
using MZikmund.Services.Account;
using MZikmund.Services.Dialogs;
using MZikmund.Services.Navigation;
using MZikmund.ViewModels;
using Refit;

namespace MZikmund.App.Core.Tests.ViewModels;

public class CommentsViewModelTests
{
	private readonly Mock<IMZikmundApi> _api = new();
	private readonly Mock<IUserService> _userService = new();
	private readonly Mock<ISignInPrompt> _signInPrompt = new();
	private readonly Mock<IDialogService> _dialogService = new();

	public CommentsViewModelTests()
	{
		TestLocalization.EnsureConfigured();
		_signInPrompt.Setup(p => p.EnsureSignedInAsync()).ReturnsAsync(true);
	}

	private CommentsViewModel CreateViewModel() =>
		new(_api.Object, _userService.Object, _signInPrompt.Object, _dialogService.Object, Mock.Of<IWindowShellProvider>(), NullLogger<CommentsViewModel>.Instance);

	private static CommentDto Comment(Guid? id = null, Guid? postId = null) => new()
	{
		Id = id ?? Guid.NewGuid(),
		PostId = postId ?? Guid.NewGuid(),
		AuthorDisplayName = "Reader",
		Content = "Hello",
		ContentHtml = "<p>Hello</p>",
		Status = CommentStatus.Visible,
		CreatedOn = DateTime.UtcNow,
		UpvoteCount = 0,
		ReplyCount = 0,
		HasUpvoted = false,
		IsOwn = false,
	};

	private static ApiResponse<T> Ok<T>(T content) => new(new HttpResponseMessage(HttpStatusCode.OK), content, new RefitSettings());

	private static ApiResponse<PagedResponse<CommentDto>> Page(params CommentDto[] comments) => Ok(new PagedResponse<CommentDto>(comments, 1, CommentLimits.PageSize, comments.Length));

	private static async Task<ApiResponse<T>> Failure<T>(HttpStatusCode status)
	{
		var message = new HttpResponseMessage(status);
		var error = await ApiException.Create(new HttpRequestMessage(), HttpMethod.Get, message, new RefitSettings());
		return new(message, default, new RefitSettings(), error);
	}

	[Fact]
	public async Task Post_InvalidatesTheSessionBeforePromptingAgain_On401()
	{
		// Arrange
		var postId = Guid.NewGuid();
		var created = Comment(postId: postId);
		_api.Setup(a => a.GetPostCommentsAsync(postId, It.IsAny<CommentSortOrder>(), It.IsAny<int>())).ReturnsAsync(Page());
		_api.SetupSequence(a => a.CreateCommentAsync(postId, It.IsAny<CreateCommentRequest>()))
			.Returns(Failure<CommentDto>(HttpStatusCode.Unauthorized))
			.ReturnsAsync(Ok(created));
		List<string> calls = new();
		_userService.Setup(u => u.InvalidateSessionAsync()).Callback(() => calls.Add("invalidate")).Returns(Task.CompletedTask);
		_signInPrompt.Setup(p => p.EnsureSignedInAsync()).Callback(() => calls.Add("prompt")).ReturnsAsync(true);
		var viewModel = CreateViewModel();
		await viewModel.LoadAsync(postId);
		viewModel.DraftText = "Hello";

		// Act
		await viewModel.PostCommand.ExecuteAsync(null);

		// Assert
		Assert.Equal("prompt,invalidate,prompt", string.Join(",", calls));
		Assert.Single(viewModel.Comments);
	}

	[Fact]
	public async Task Load_FallsBackToTheAnonymousRead_WhenTheAuthenticatedReadIs401()
	{
		// Arrange
		var postId = Guid.NewGuid();
		_userService.SetupGet(u => u.IsLoggedIn).Returns(true);
		_api.Setup(a => a.GetPostCommentsAuthenticatedAsync(postId, It.IsAny<CommentSortOrder>(), It.IsAny<int>()))
			.Returns(Failure<PagedResponse<CommentDto>>(HttpStatusCode.Unauthorized));
		_api.Setup(a => a.GetPostCommentsAsync(postId, It.IsAny<CommentSortOrder>(), It.IsAny<int>())).ReturnsAsync(Page(Comment(postId: postId)));
		var viewModel = CreateViewModel();

		// Act
		await viewModel.LoadAsync(postId);

		// Assert
		Assert.False(viewModel.HasError);
		Assert.Single(viewModel.Comments);
	}

	[Fact]
	public async Task Post_DoesNotLandInAnotherPost_WhenTheViewModelMovedOnWhileWaitingForTheReload()
	{
		// Arrange
		var postA = Guid.NewGuid();
		var postB = Guid.NewGuid();
		TaskCompletionSource<ApiResponse<PagedResponse<CommentDto>>> pendingA = new();
		var existingB = Comment(postId: postB);
		_api.Setup(a => a.GetPostCommentsAsync(postA, It.IsAny<CommentSortOrder>(), It.IsAny<int>())).Returns(pendingA.Task);
		_api.Setup(a => a.GetPostCommentsAsync(postB, It.IsAny<CommentSortOrder>(), It.IsAny<int>())).ReturnsAsync(Page(existingB));
		_api.Setup(a => a.CreateCommentAsync(postA, It.IsAny<CreateCommentRequest>())).ReturnsAsync(Ok(Comment(postId: postA)));
		var viewModel = CreateViewModel();
		var loadA = viewModel.LoadAsync(postA);
		viewModel.DraftText = "Hello";
		var posting = viewModel.PostCommand.ExecuteAsync(null);

		// Act
		await viewModel.LoadAsync(postB);
		pendingA.SetResult(Page());
		await posting;
		await loadA;

		// Assert
		Assert.Equal(existingB.Id, Assert.Single(viewModel.Comments).Id);
		Assert.Equal(1, viewModel.TotalCount);
	}

	[Fact]
	public async Task Load_ResetsTheCount_WhenAnotherPostStartsLoading()
	{
		// Arrange
		var postA = Guid.NewGuid();
		var postB = Guid.NewGuid();
		TaskCompletionSource<ApiResponse<PagedResponse<CommentDto>>> pendingB = new();
		_api.Setup(a => a.GetPostCommentsAsync(postA, It.IsAny<CommentSortOrder>(), It.IsAny<int>())).ReturnsAsync(Page(Comment(), Comment()));
		_api.Setup(a => a.GetPostCommentsAsync(postB, It.IsAny<CommentSortOrder>(), It.IsAny<int>())).Returns(pendingB.Task);
		var viewModel = CreateViewModel();
		await viewModel.LoadAsync(postA);
		Assert.Equal(2, viewModel.TotalCount);

		// Act
		var loadB = viewModel.LoadAsync(postB);

		// Assert
		Assert.Equal(0, viewModel.TotalCount);
		pendingB.SetResult(Page());
		await loadB;
	}

	[Fact]
	public async Task Post_ExplainsTheAppRateLimit_On429()
	{
		// Arrange
		var postId = Guid.NewGuid();
		_api.Setup(a => a.GetPostCommentsAsync(postId, It.IsAny<CommentSortOrder>(), It.IsAny<int>())).ReturnsAsync(Page());
		_api.Setup(a => a.CreateCommentAsync(postId, It.IsAny<CreateCommentRequest>()))
			.Returns(Failure<CommentDto>(HttpStatusCode.TooManyRequests));
		var viewModel = CreateViewModel();
		await viewModel.LoadAsync(postId);
		viewModel.DraftText = "Hello";

		// Act
		await viewModel.PostCommand.ExecuteAsync(null);

		// Assert
		_dialogService.Verify(d => d.ShowStatusMessageAsync(StatusMessageDialogType.Error, "CommentPostFailed", "CommentAppRateLimited"), Times.Once);
		_userService.Verify(u => u.InvalidateSessionAsync(), Times.Never);
		Assert.Equal("Hello", viewModel.DraftText);
	}

	[Fact]
	public async Task CreateComment_SendsTheAppClientHeader()
	{
		// Arrange
		CapturingHandler handler = new();
		RefitSettings settings = new() { AuthorizationHeaderValueGetter = (_, _) => Task.FromResult("token") };
		var api = RestService.For<IMZikmundApi>(new HttpClient(handler) { BaseAddress = new Uri("https://localhost") }, settings);

		// Act
		await api.CreateCommentAsync(Guid.NewGuid(), new CreateCommentRequest { Content = "Hello" });

		// Assert
		Assert.Equal("app", Assert.Single(handler.Request!.Headers.GetValues("X-MZikmund-Client")));
	}

	private sealed class CapturingHandler : HttpMessageHandler
	{
		public HttpRequestMessage? Request { get; private set; }

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			Request = request;
			return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError) { RequestMessage = request });
		}
	}
}
