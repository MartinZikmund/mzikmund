using Microsoft.Extensions.Options;
using MZikmund.Api.Client;
using MZikmund.Business.Models;
using MZikmund.DataContracts.Blog;
using Launcher = Windows.System.Launcher;

namespace MZikmund.ViewModels;

public partial class PostViewModel : PageViewModel
{
	private readonly IMZikmundApi _api;
	private readonly IOptions<AppConfig> _appConfig;
	private readonly ILogger<PostViewModel> _logger;
	private Guid _postId;

	public PostViewModel(IMZikmundApi api, IOptions<AppConfig> appConfig, ILogger<PostViewModel> logger)
	{
		_api = api;
		_appConfig = appConfig;
		_logger = logger;
	}

	public override string Title => Post?.Title ?? "";

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(Title))]
	[NotifyCanExecuteChangedFor(nameof(OpenInBrowserCommand))]
	public partial Post? Post { get; set; }

	[ObservableProperty]
	public partial Uri? EmbedUrl { get; set; }

	[ObservableProperty]
	public partial bool IsLoading { get; set; }

	[ObservableProperty]
	public partial bool HasError { get; set; }

	private Uri WebRoot => new((_appConfig.Value.WebUrl ?? throw new InvalidOperationException("WebUrl is not set in configuration")).TrimEnd('/') + "/");

	public override void ViewNavigatedTo(object? parameter)
	{
		if (parameter is not Guid postId)
		{
			return;
		}

		_postId = postId;
		IsLoading = true;
		HasError = false;

		// The embed renders the hero itself, so the page can start loading before the API answers.
		EmbedUrl = new Uri(WebRoot, $"embed/post/{postId}");
		_ = LoadPostAsync();
	}

	private async Task LoadPostAsync()
	{
		try
		{
			var response = await _api.GetPostAsync(_postId);
			Post = response.Content;
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Loading post metadata failed");
		}
	}

	public void OnContentLoaded(bool isSuccess)
	{
		IsLoading = false;
		HasError = !isSuccess;
	}

	[RelayCommand]
	private void Retry()
	{
		IsLoading = true;
		HasError = false;
		var url = EmbedUrl;
		EmbedUrl = null;
		EmbedUrl = url;
	}

	private bool CanOpenInBrowser() => Post is not null;

	[RelayCommand(CanExecute = nameof(CanOpenInBrowser))]
	private async Task OpenInBrowserAsync() =>
		await Launcher.LaunchUriAsync(new Uri(WebRoot, $"blog/{Post!.RouteName}"));
}
