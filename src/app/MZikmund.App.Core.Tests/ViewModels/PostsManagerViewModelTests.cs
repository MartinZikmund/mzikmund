using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MZikmund.Api.Client;
using MZikmund.DataContracts;
using MZikmund.DataContracts.Blog;
using MZikmund.Services.Dialogs;
using MZikmund.Services.Navigation;
using MZikmund.ViewModels.Admin;
using Refit;

namespace MZikmund.App.Core.Tests.ViewModels;

public class PostsManagerViewModelTests
{
	private const int PageSize = 1;

	private readonly Mock<IMZikmundApi> _api = new();

	private PostsManagerViewModel CreateViewModel() =>
		new(_api.Object, Mock.Of<IDialogService>(), Mock.Of<INavigationService>(), NullLogger<PostsManagerViewModel>.Instance);

	private static ApiResponse<PagedResponse<PostListItem>> Page(int pageNumber, int totalCount, params string[] titles)
	{
		var posts = titles.Select(title => new PostListItem { Id = Guid.NewGuid(), Title = title, RouteName = title });
		return new(new HttpResponseMessage(HttpStatusCode.OK), new(posts, pageNumber, PageSize, totalCount), new RefitSettings());
	}

	private static TaskCompletionSource<ApiResponse<PagedResponse<PostListItem>>> PendingPage() => new();

	[Fact]
	public async Task OverlappingRefreshes_DoNotDuplicatePosts()
	{
		var first = PendingPage();
		var second = PendingPage();
		_api.SetupSequence(a => a.GetAllPostsAsync(1))
			.Returns(first.Task)
			.Returns(second.Task);

		var viewModel = CreateViewModel();
		var firstRefresh = viewModel.RefreshCommand.ExecuteAsync(null);
		var secondRefresh = viewModel.RefreshCommand.ExecuteAsync(null);

		second.SetResult(Page(1, 1, "Post"));
		first.SetResult(Page(1, 1, "Post"));
		await Task.WhenAll(firstRefresh, secondRefresh);

		Assert.Equal("Post", Assert.Single(viewModel.VisiblePosts).Title);
		Assert.False(viewModel.IsLoading);
	}

	[Fact]
	public async Task StaleRefresh_DoesNotEndTheCurrentLoadingState()
	{
		var first = PendingPage();
		var second = PendingPage();
		_api.SetupSequence(a => a.GetAllPostsAsync(1))
			.Returns(first.Task)
			.Returns(second.Task);

		var viewModel = CreateViewModel();
		var firstRefresh = viewModel.RefreshCommand.ExecuteAsync(null);
		var secondRefresh = viewModel.RefreshCommand.ExecuteAsync(null);

		first.SetException(new HttpRequestException("offline"));
		await firstRefresh;

		Assert.True(viewModel.IsLoading);
		Assert.False(viewModel.HasError);

		second.SetResult(Page(1, 1, "Post"));
		await secondRefresh;

		Assert.False(viewModel.IsLoading);
		Assert.False(viewModel.HasError);
	}

	[Fact]
	public async Task FailedRefresh_ForgetsThePreviousPaging()
	{
		_api.SetupSequence(a => a.GetAllPostsAsync(1))
			.ReturnsAsync(Page(1, 5, "Post"))
			.ThrowsAsync(new HttpRequestException("offline"));

		var viewModel = CreateViewModel();
		await viewModel.RefreshCommand.ExecuteAsync(null);
		Assert.True(viewModel.CanLoadMore);

		await viewModel.RefreshCommand.ExecuteAsync(null);

		Assert.True(viewModel.HasError);
		Assert.False(viewModel.CanLoadMore);
		Assert.Empty(viewModel.VisiblePosts);
	}

	[Fact]
	public async Task LoadMore_ThatLandsAfterARefresh_IsDiscarded()
	{
		var stalePage = PendingPage();
		_api.SetupSequence(a => a.GetAllPostsAsync(1))
			.ReturnsAsync(Page(1, 2, "First"))
			.ReturnsAsync(Page(1, 2, "First"));
		_api.Setup(a => a.GetAllPostsAsync(2)).Returns(stalePage.Task);

		var viewModel = CreateViewModel();
		await viewModel.RefreshCommand.ExecuteAsync(null);

		var loadMore = viewModel.LoadMoreCommand.ExecuteAsync(null);
		await viewModel.RefreshCommand.ExecuteAsync(null);

		stalePage.SetResult(Page(2, 2, "Second"));
		await loadMore;

		Assert.Equal("First", Assert.Single(viewModel.VisiblePosts).Title);
		Assert.Equal(1, viewModel.CurrentPage);
		Assert.True(viewModel.CanLoadMore);
	}

	[Fact]
	public async Task Search_FindsPostsOnPagesThatWereNotLoadedYet()
	{
		_api.Setup(a => a.GetAllPostsAsync(1)).ReturnsAsync(Page(1, 3, "Newest"));
		_api.Setup(a => a.GetAllPostsAsync(2)).ReturnsAsync(Page(2, 3, "Middle"));
		_api.Setup(a => a.GetAllPostsAsync(3)).ReturnsAsync(Page(3, 3, "Oldest"));

		var viewModel = CreateViewModel();
		await viewModel.RefreshCommand.ExecuteAsync(null);

		viewModel.SearchText = "oldest";
		await viewModel.LoadMoreCommand.ExecutionTask!;

		Assert.Equal("Oldest", Assert.Single(viewModel.VisiblePosts).Title);
		Assert.Equal(3, viewModel.CurrentPage);
	}

	[Fact]
	public async Task Search_ReportsNoResults_OnlyOnceEveryPageWasChecked()
	{
		var lastPage = PendingPage();
		_api.Setup(a => a.GetAllPostsAsync(1)).ReturnsAsync(Page(1, 2, "Newest"));
		_api.Setup(a => a.GetAllPostsAsync(2)).Returns(lastPage.Task);

		var viewModel = CreateViewModel();
		await viewModel.RefreshCommand.ExecuteAsync(null);

		viewModel.SearchText = "nothing matches";
		Assert.False(viewModel.IsEmpty);

		lastPage.SetResult(Page(2, 2, "Oldest"));
		await viewModel.LoadMoreCommand.ExecutionTask!;

		Assert.True(viewModel.IsEmpty);
	}

	[Fact]
	public async Task NoSearch_LoadsOnlyTheFirstPage()
	{
		_api.Setup(a => a.GetAllPostsAsync(1)).ReturnsAsync(Page(1, 3, "Newest"));

		var viewModel = CreateViewModel();
		await viewModel.RefreshCommand.ExecuteAsync(null);

		_api.Verify(a => a.GetAllPostsAsync(It.IsNotIn(1)), Times.Never);
	}
}
