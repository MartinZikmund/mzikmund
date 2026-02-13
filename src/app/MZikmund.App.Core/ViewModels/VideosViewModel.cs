using System.Collections.ObjectModel;
using System.Windows.Input;
using MZikmund.Api.Client;
using MZikmund.DataContracts.Videos;
using MZikmund.Services.Loading;
using MZikmund.Services.Localization;

namespace MZikmund.ViewModels;

public class VideosViewModel : PageViewModel
{
	private readonly IMZikmundApi _api;
	private readonly ILoadingIndicator _loadingIndicator;
	private readonly ILogger<VideosViewModel> _logger;
	private bool _isLoading;

	public VideosViewModel(
		IMZikmundApi api,
		ILoadingIndicator loadingIndicator,
		ILogger<VideosViewModel> logger)
	{
		_api = api ?? throw new ArgumentNullException(nameof(api));
		_loadingIndicator = loadingIndicator;
		_logger = logger;
		RefreshCommand = new AsyncRelayCommand(RefreshAsync);
	}

	public override string Title => Localizer.Instance.GetString("Videos") ?? "Videos";

	public ObservableCollection<VideoDto> Videos { get; } = new();

	public bool IsLoading
	{
		get => _isLoading;
		private set => SetProperty(ref _isLoading, value);
	}

	public ICommand RefreshCommand { get; }

	public override async void ViewNavigatedTo(object? parameter)
	{
		base.ViewNavigatedTo(parameter);
		await LoadVideosAsync();
	}

	private async Task LoadVideosAsync()
	{
		using var _ = _loadingIndicator.BeginLoading();
		IsLoading = true;
		try
		{
			var response = await _api.GetVideosAsync();
			if (response.Content != null)
			{
				Videos.Clear();
				foreach (var video in response.Content)
				{
					Videos.Add(video);
				}
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to load videos");
		}
		finally
		{
			IsLoading = false;
		}
	}

	private async Task RefreshAsync()
	{
		await LoadVideosAsync();
	}
}
