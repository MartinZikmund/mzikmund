using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using MZikmund.Api.Client;
using MZikmund.Business.Models;
using MZikmund.DataContracts.Blog;
using MZikmund.ViewModels;
using Refit;

namespace MZikmund.App.Core.Tests.ViewModels;

public class PostViewModelTests
{
	private readonly Mock<IMZikmundApi> _api = new();

	private PostViewModel CreateViewModel() =>
		new(_api.Object, Options.Create(new AppConfig { WebUrl = "https://example.test" }), NullLogger<PostViewModel>.Instance);

	private static ApiResponse<Post> PostResponse(Post? post, HttpStatusCode status = HttpStatusCode.OK) =>
		new(new HttpResponseMessage(status), post, new RefitSettings());

	[Theory]
	[InlineData(true, 200, false)]
	[InlineData(true, 0, false)]
	[InlineData(true, 404, true)]
	[InlineData(true, 500, true)]
	[InlineData(false, 200, true)]
	[InlineData(false, 0, true)]
	public void OnContentLoaded_TreatsHttpErrorsAsFailures(bool isSuccess, int httpStatusCode, bool expectedError)
	{
		var viewModel = CreateViewModel();

		viewModel.OnContentLoaded(isSuccess, httpStatusCode);

		Assert.Equal(expectedError, viewModel.HasError);
		Assert.False(viewModel.IsLoading);
	}

	[Fact]
	public void Retry_RequestsReload_BecauseTheEmbedUrlDoesNotChange()
	{
		_api.Setup(a => a.GetPostAsync(It.IsAny<Guid>())).ReturnsAsync(PostResponse(new Post()));
		var viewModel = CreateViewModel();
		viewModel.ViewNavigatedTo(Guid.NewGuid());
		viewModel.OnContentLoaded(isSuccess: false);

		var reloads = 0;
		viewModel.ReloadRequested += (_, _) => reloads++;

		viewModel.RetryCommand.Execute(null);

		Assert.Equal(1, reloads);
		Assert.True(viewModel.IsLoading);
		Assert.False(viewModel.HasError);
	}

	[Fact]
	public void Retry_ReloadsPostMetadata_WhenTheFirstLoadFailed()
	{
		Post loaded = new() { Title = "Loaded" };
		_api.SetupSequence(a => a.GetPostAsync(It.IsAny<Guid>()))
			.ThrowsAsync(new HttpRequestException("offline"))
			.ReturnsAsync(PostResponse(loaded));

		var viewModel = CreateViewModel();
		viewModel.ViewNavigatedTo(Guid.NewGuid());
		Assert.Null(viewModel.Post);

		viewModel.RetryCommand.Execute(null);

		Assert.Same(loaded, viewModel.Post);
	}

	[Fact]
	public void Retry_DoesNotRefetchPostMetadata_WhenItIsAlreadyLoaded()
	{
		_api.Setup(a => a.GetPostAsync(It.IsAny<Guid>())).ReturnsAsync(PostResponse(new Post()));
		var viewModel = CreateViewModel();
		viewModel.ViewNavigatedTo(Guid.NewGuid());

		viewModel.RetryCommand.Execute(null);

		_api.Verify(a => a.GetPostAsync(It.IsAny<Guid>()), Times.Once);
	}
}
