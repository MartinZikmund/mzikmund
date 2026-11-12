using System.Collections.ObjectModel;
using MZikmund.Api.Client;
using MZikmund.Services.Localization;
using MZikmund.Services.Navigation;
using MZikmund.ViewModels.Items;
using MZikmund.Web.Core.Services;
using Launcher = Windows.System.Launcher;

namespace MZikmund.ViewModels;

public partial class HomeViewModel : PageViewModel
{
	private const int LatestCount = 3;

	private readonly IMZikmundApi _api;
	private readonly IMarkdownConverter _markdownConverter;
	private readonly INavigationService _navigationService;
	private readonly ILogger<HomeViewModel> _logger;
	private bool _hasLoaded;

	public HomeViewModel(
		IMZikmundApi api,
		IMarkdownConverter markdownConverter,
		INavigationService navigationService,
		ILogger<HomeViewModel> logger)
	{
		_api = api;
		_markdownConverter = markdownConverter;
		_navigationService = navigationService;
		_logger = logger;
	}

	public override string Title => Localizer.Instance.GetString("Home");

	public ObservableCollection<PostListItemViewModel> LatestPosts { get; } = new();

	public ObservableCollection<VideoCardViewModel> LatestVideos { get; } = new();

	[ObservableProperty]
	public partial bool IsLoadingPosts { get; set; }

	[ObservableProperty]
	public partial bool HasPostsError { get; set; }

	[ObservableProperty]
	public partial bool HasVideos { get; set; }

	public override async void ViewNavigatedTo(object? parameter)
	{
		base.ViewNavigatedTo(parameter);
		if (_hasLoaded)
		{
			return;
		}

		_hasLoaded = true;
		await Task.WhenAll(LoadPostsAsync(), LoadVideosAsync());
	}

	[RelayCommand]
	private async Task LoadPostsAsync()
	{
		IsLoadingPosts = true;
		HasPostsError = false;
		try
		{
			var response = await _api.GetPostsAsync(1);
			if (response.Content is { } content)
			{
				LatestPosts.Clear();
				foreach (var post in content.Data.Take(LatestCount))
				{
					LatestPosts.Add(new PostListItemViewModel(post, _markdownConverter));
				}
			}
			else
			{
				_logger.LogWarning("Loading latest posts returned {StatusCode}", response.StatusCode);
				HasPostsError = true;
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Loading latest posts failed");
			HasPostsError = true;
		}
		finally
		{
			IsLoadingPosts = false;
		}
	}

	private async Task LoadVideosAsync()
	{
		try
		{
			var response = await _api.GetLatestVideosAsync(LatestCount);
			if (response.Content is { } videos)
			{
				LatestVideos.Clear();
				foreach (var video in videos.Take(LatestCount))
				{
					LatestVideos.Add(new VideoCardViewModel(video));
				}
			}
		}
		catch (Exception ex)
		{
			// Videos are a bonus strip; the page stays useful without them.
			_logger.LogWarning(ex, "Loading latest videos failed");
		}

		HasVideos = LatestVideos.Count > 0;
	}

	public void OpenPost(Guid postId) => _navigationService.Navigate<PostViewModel>(postId);

	public static async Task OpenVideoAsync(VideoCardViewModel video)
	{
		if (video.Url is { } url)
		{
			await Launcher.LaunchUriAsync(url);
		}
	}

	[RelayCommand]
	private void BrowseBlog() => _navigationService.Navigate<BlogViewModel>();

	[RelayCommand]
	private void BrowseVideos() => _navigationService.Navigate<VideosViewModel>();

	[RelayCommand]
	private static async Task OpenGitHubAsync() =>
		await Launcher.LaunchUriAsync(new Uri("https://github.com/MartinZikmund"));
}
