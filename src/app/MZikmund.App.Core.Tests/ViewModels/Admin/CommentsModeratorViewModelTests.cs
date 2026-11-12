using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MZikmund.Api.Client;
using MZikmund.DataContracts;
using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Services.Comments;
using MZikmund.Services.Dialogs;
using MZikmund.ViewModels.Admin;
using Refit;

namespace MZikmund.App.Core.Tests.ViewModels.Admin;

public class CommentsModeratorViewModelTests
{
	private const int PageSize = 1;

	private readonly Mock<IMZikmundApi> _api = new();

	public CommentsModeratorViewModelTests()
	{
		TestLocalization.EnsureConfigured();
		_api.Setup(a => a.GetCommentReportsAsync(It.IsAny<int>()))
			.ReturnsAsync(Ok(new PagedResponse<CommentReportDto>(Array.Empty<CommentReportDto>(), 1, PageSize, 0)));
	}

	private CommentsModeratorViewModel CreateViewModel() =>
		new(_api.Object, Mock.Of<IDialogService>(), Mock.Of<ICommentsBadgeService>(), NullLogger<CommentsModeratorViewModel>.Instance);

	private static ApiResponse<T> Ok<T>(T content) => new(new HttpResponseMessage(HttpStatusCode.OK), content, new RefitSettings());

	private static CommentModerationDto Entry() => new()
	{
		Id = Guid.NewGuid(),
		PostId = Guid.NewGuid(),
		AuthorId = Guid.NewGuid(),
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

	private static ApiResponse<PagedResponse<CommentModerationDto>> FeedPage(int pageNumber, int totalCount, params CommentModerationDto[] entries) =>
		Ok(new PagedResponse<CommentModerationDto>(entries, pageNumber, PageSize, totalCount));

	[Fact]
	public async Task LoadMore_DiscardsAPageThatArrivesAfterARefresh()
	{
		// Arrange
		var first = Entry();
		var refreshed = Entry();
		TaskCompletionSource<ApiResponse<PagedResponse<CommentModerationDto>>> stalePage = new();
		_api.SetupSequence(a => a.GetModerationFeedAsync(null, 1))
			.ReturnsAsync(FeedPage(1, 2, first))
			.ReturnsAsync(FeedPage(1, 2, refreshed));
		_api.Setup(a => a.GetModerationFeedAsync(null, 2)).Returns(stalePage.Task);
		var viewModel = CreateViewModel();
		await viewModel.RefreshCommand.ExecuteAsync(null);
		var loadingMore = viewModel.LoadMoreCommand.ExecuteAsync(null);

		// Act
		await viewModel.RefreshCommand.ExecuteAsync(null);
		stalePage.SetResult(FeedPage(2, 2, Entry()));
		await loadingMore;

		// Assert
		Assert.Equal(refreshed.Id, Assert.Single(viewModel.Feed).Id);
		Assert.False(viewModel.IsLoadingMore);
	}

	[Fact]
	public async Task CanLoadMore_IsFalseWhileRefreshing()
	{
		// Arrange
		TaskCompletionSource<ApiResponse<PagedResponse<CommentModerationDto>>> pending = new();
		_api.SetupSequence(a => a.GetModerationFeedAsync(null, 1))
			.ReturnsAsync(FeedPage(1, 2, Entry()))
			.Returns(pending.Task);
		var viewModel = CreateViewModel();
		await viewModel.RefreshCommand.ExecuteAsync(null);
		Assert.True(viewModel.CanLoadMore);

		// Act
		var refreshing = viewModel.RefreshCommand.ExecuteAsync(null);

		// Assert
		Assert.False(viewModel.CanLoadMore);
		pending.SetResult(FeedPage(1, 2, Entry()));
		await refreshing;
		Assert.True(viewModel.CanLoadMore);
	}
}
