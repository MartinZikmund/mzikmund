using System.Collections.ObjectModel;
using MZikmund.Api.Client;
using MZikmund.Services.Localization;
using MZikmund.ViewModels.Items;
using Launcher = Windows.System.Launcher;

namespace MZikmund.ViewModels;

public partial class VideosViewModel : PageViewModel
{
	private readonly IMZikmundApi _api;
	private readonly ILogger<VideosViewModel> _logger;

	public VideosViewModel(IMZikmundApi api, ILogger<VideosViewModel> logger)
	{
		_api = api ?? throw new ArgumentNullException(nameof(api));
		_logger = logger;
	}

	public override string Title => Localizer.Instance.GetString("Videos");

	public string Lead => Localizer.Instance.GetString("VideosLead");

	public ObservableCollection<VideoCardViewModel> Videos { get; } = new();

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsEmpty))]
	public partial bool IsLoading { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsEmpty))]
	public partial bool HasError { get; set; }

	public bool IsEmpty => !IsLoading && !HasError && Videos.Count == 0;

	public override async void ViewNavigatedTo(object? parameter)
	{
		base.ViewNavigatedTo(parameter);
		if (Videos.Count == 0 && !IsLoading)
		{
			await RefreshAsync();
		}
	}

	[RelayCommand]
	private async Task RefreshAsync()
	{
		IsLoading = true;
		HasError = false;
		try
		{
			var response = await _api.GetVideosAsync();
			if (response.IsSuccessStatusCode && response.Content != null)
			{
				Videos.Clear();
				foreach (var video in response.Content)
				{
					Videos.Add(new VideoCardViewModel(video));
				}
			}
			else
			{
				_logger.LogWarning("Failed to load videos. Status: {StatusCode}", response.StatusCode);
				HasError = Videos.Count == 0;
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to load videos");
			HasError = Videos.Count == 0;
		}
		finally
		{
			IsLoading = false;
		}
	}

	public static async Task OpenVideoAsync(VideoCardViewModel video)
	{
		if (video.Url is { } url)
		{
			await Launcher.LaunchUriAsync(url);
		}
	}
}
