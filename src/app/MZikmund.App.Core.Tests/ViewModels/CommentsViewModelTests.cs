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
}
