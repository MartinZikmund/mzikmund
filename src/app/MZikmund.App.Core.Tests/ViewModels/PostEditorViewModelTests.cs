using System.Net;
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

	public PostEditorViewModelTests() => TestLocalization.EnsureConfigured();

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

	private static PostAdmin Live() => new() { Id = Guid.NewGuid(), Title = "Live", RouteName = "live", IsPublished = true, PublishedDate = DateTimeOffset.UtcNow.AddDays(-1) };

	private static PostAdmin Scheduled() => new() { Id = Guid.NewGuid(), Title = "Scheduled", RouteName = "scheduled", IsPublished = true, PublishedDate = DateTimeOffset.UtcNow.AddDays(3) };

	[Fact]
	public void ScheduledPost_CountsDownInsteadOfClaimingToBeLive()
	{
		var viewModel = OpenPost(Scheduled());

		Assert.True(viewModel.IsScheduled);
		Assert.False(viewModel.IsLive);
		Assert.False(viewModel.CanChangePublished);
		Assert.StartsWith("Goes live in", viewModel.ScheduledCountdown);
	}

	[Fact]
	public void LivePost_IsNotScheduled()
	{
		var viewModel = OpenPost(Live());

		Assert.False(viewModel.IsScheduled);
		Assert.True(viewModel.IsLive);
		Assert.Equal("", viewModel.ScheduledCountdown);
	}

	[Fact]
	public void ScheduledPost_BecomesLiveWhenItsDatePasses()
	{
		var post = Scheduled();
		var viewModel = OpenPost(post);

		viewModel.UpdateSchedule(post.PublishedDate!.Value.AddSeconds(1));

		Assert.False(viewModel.IsScheduled);
		Assert.True(viewModel.IsLive);
		Assert.Equal("", viewModel.ScheduledCountdown);
	}

	[Fact]
	public async Task SchedulingADraft_StartsTheCountdownOnceTheServerConfirms()
	{
		var post = Draft();
		var goesLive = DateTimeOffset.UtcNow.AddDays(3);
		var viewModel = OpenPost(post);
		_api.Setup(a => a.UpdatePostAsync(post.Id, It.IsAny<Post>())).ReturnsAsync(Ok<Post>(new() { Id = post.Id, IsPublished = true, PublishedDate = goesLive }));
		_api.Setup(a => a.GetPostForAdminAsync(post.Id)).ReturnsAsync(Ok(new PostAdmin { Id = post.Id, RouteName = post.RouteName, IsPublished = true, PublishedDate = goesLive }));

		viewModel.IsPublished = true;
		Assert.False(viewModel.IsScheduled);

		await viewModel.SaveCommand.ExecuteAsync(null);

		Assert.True(viewModel.IsScheduled);
		Assert.False(viewModel.IsLive);
	}

	[Fact]
	public async Task FailedSave_DoesNotStartACountdown()
	{
		var post = Draft();
		var viewModel = OpenPost(post);
		_api.Setup(a => a.UpdatePostAsync(post.Id, It.IsAny<Post>())).ReturnsAsync(Failed<Post>());

		viewModel.IsPublished = true;
		viewModel.PublishDateValue = DateTimeOffset.Now.AddDays(3);
		await viewModel.SaveCommand.ExecuteAsync(null);

		Assert.False(viewModel.IsScheduled);
	}

	[Fact]
	public async Task Save_SendsTheOffsetOfThePickedDateRatherThanToday()
	{
		var post = Draft();
		var viewModel = OpenPost(post);
		Post? sent = null;
		_api.Setup(a => a.UpdatePostAsync(post.Id, It.IsAny<Post>()))
			.Callback((Guid _, Post body) => sent = body)
			.ReturnsAsync(Ok<Post>(new() { Id = post.Id }));

		// Half a year out lands on the other side of the clock change in zones that have one.
		var picked = DateTime.Today.AddMonths(6);
		viewModel.PublishDateValue = new DateTimeOffset(picked);
		viewModel.PublishTime = new TimeSpan(9, 0, 0);
		await viewModel.SaveCommand.ExecuteAsync(null);

		var local = picked.AddHours(9);
		Assert.Equal(new DateTimeOffset(local, TimeZoneInfo.Local.GetUtcOffset(local)), sent!.PublishedDate);
	}

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
