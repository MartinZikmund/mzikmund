using System.Net;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using MZikmund.Api.Client;
using MZikmund.Business.Models;
using MZikmund.DataContracts.Blog;
using MZikmund.Services.Dialogs;
using MZikmund.Services.Loading;
using MZikmund.Services.Navigation;
using MZikmund.Services.Timers;
using MZikmund.Shared.Extensions;
using MZikmund.ViewModels.Admin;
using MZikmund.Web.Core.Services;
using Refit;

namespace MZikmund.App.Core.Tests.ViewModels;

public class PostEditorViewModelTests
{
	private readonly Mock<IMZikmundApi> _api = new();

	// The save error path reaches the static Localizer, which resolves through Ioc.Default.
	static PostEditorViewModelTests() =>
		Ioc.Default.ConfigureServices(new ServiceCollection()
			.AddSingleton(Mock.Of<IStringLocalizer>())
			.BuildServiceProvider());

	private PostEditorViewModel CreateViewModel() =>
		new(
			_api.Object,
			Mock.Of<IWindowShellProvider>(),
			Mock.Of<IDialogService>(),
			Mock.Of<ILoadingIndicator>(),
			Mock.Of<IPostContentProcessor>(),
			Mock.Of<ITimerFactory>(),
			Options.Create(new AppConfig { WebUrl = "https://example.test" }),
			NullLogger<PostEditorViewModel>.Instance,
			NullLogger<MediaBrowserDialogViewModel>.Instance);

	private static ApiResponse<T> Ok<T>(T content) =>
		new(new HttpResponseMessage(HttpStatusCode.OK), content, new RefitSettings());

	private static ApiResponse<T> Failed<T>() where T : class =>
		new(new HttpResponseMessage(HttpStatusCode.InternalServerError), null, new RefitSettings());

	private PostEditorViewModel OpenPost(PostAdmin post)
	{
		_api.Setup(a => a.GetPostForAdminAsync(post.Id)).ReturnsAsync(Ok(post));
		var viewModel = CreateViewModel();
		viewModel.ViewNavigatedTo(post.Id);
		return viewModel;
	}

	private static PostAdmin Draft() => new() { Id = Guid.NewGuid(), Title = "Draft", RouteName = "draft" };

	private static PostAdmin Live() => new() { Id = Guid.NewGuid(), Title = "Live", RouteName = "live", IsPublished = true };

	[Fact]
	public void LivePost_LocksThePublishedToggle() =>
		Assert.False(OpenPost(Live()).CanChangePublished);

	[Fact]
	public void Draft_KeepsThePublishedToggleUnlocked() =>
		Assert.True(OpenPost(Draft()).CanChangePublished);

	[Fact]
	public async Task PublishingADraft_LocksTheToggleOnceTheServerConfirms()
	{
		var post = Draft();
		var viewModel = OpenPost(post);
		_api.Setup(a => a.UpdatePostAsync(post.Id, It.IsAny<Post>())).ReturnsAsync(Ok<Post>(new() { Id = post.Id, IsPublished = true }));
		_api.Setup(a => a.GetPostForAdminAsync(post.Id)).ReturnsAsync(Ok(new PostAdmin { Id = post.Id, RouteName = post.RouteName, IsPublished = true }));

		viewModel.IsPublished = true;
		await viewModel.SaveCommand.ExecuteAsync(null);

		Assert.False(viewModel.CanChangePublished);
		Assert.False(viewModel.IsDirty);
	}

	[Fact]
	public async Task FailedSave_LeavesTheToggleUnlocked()
	{
		var post = Draft();
		var viewModel = OpenPost(post);
		_api.Setup(a => a.UpdatePostAsync(post.Id, It.IsAny<Post>())).ReturnsAsync(Failed<Post>());

		viewModel.IsPublished = true;
		await viewModel.SaveCommand.ExecuteAsync(null);

		Assert.True(viewModel.CanChangePublished);
		Assert.True(viewModel.IsDirty);
	}

	[Fact]
	public async Task Save_ShowsThePublishedStateTheServerKept()
	{
		var post = Draft();
		var viewModel = OpenPost(post);
		_api.Setup(a => a.UpdatePostAsync(post.Id, It.IsAny<Post>())).ReturnsAsync(Ok<Post>(new() { Id = post.Id }));
		_api.Setup(a => a.GetPostForAdminAsync(post.Id)).ReturnsAsync(Ok(new PostAdmin { Id = post.Id, RouteName = post.RouteName, IsPublished = false }));

		viewModel.IsPublished = true;
		await viewModel.SaveCommand.ExecuteAsync(null);

		Assert.False(viewModel.IsPublished);
		Assert.False(viewModel.IsDirty);
	}

	[Fact]
	public async Task FailedPublish_LetsTheRouteKeepFollowingTheTitle()
	{
		var post = Draft();
		var viewModel = OpenPost(post);
		_api.Setup(a => a.UpdatePostAsync(post.Id, It.IsAny<Post>())).ReturnsAsync(Failed<Post>());

		viewModel.IsPublished = true;
		await viewModel.SaveCommand.ExecuteAsync(null);

		viewModel.PostTitle = "Renamed";

		Assert.Equal("Renamed".GenerateRouteName(), viewModel.PostRouteName);
	}
}
