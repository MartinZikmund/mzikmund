using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.UI.Dispatching;
using MZikmund.Api.Client;
using MZikmund.Business.Models;
using MZikmund.DataContracts.Blobs;
using MZikmund.Extensions;
using MZikmund.Models.Dialogs;
using MZikmund.Services.Dialogs;
using MZikmund.Services.Loading;
using MZikmund.Services.Localization;
using MZikmund.Services.Navigation;
using MZikmund.ViewModels.Items;
using Refit;
using Windows.Storage.Pickers;

namespace MZikmund.ViewModels.Admin;

public partial class MediaBrowserViewModel : PageViewModel
{
	private readonly IDialogService _dialogService;
	private readonly ILoadingIndicator _loadingIndicator;
	private readonly IMZikmundApi _api;
	private readonly IWindowShellProvider _windowShellProvider;
	private readonly IOptions<AppConfig> _appConfig;
	private readonly ILogger<ImageVariantsDialogViewModel> _imageVariantsLogger;
	private readonly DispatcherQueueTimer _debounceTimer;

	private const int PageSize = 50;
	private int _currentPage = 1;
	private bool _hasMoreItems = true;

	public MediaBrowserViewModel(
		IMZikmundApi api,
		IDialogService dialogService,
		ILoadingIndicator loadingIndicator,
		IWindowShellProvider windowShellProvider,
		IOptions<AppConfig> appConfig,
		ILogger<ImageVariantsDialogViewModel> imageVariantsLogger)
	{
		_dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
		_loadingIndicator = loadingIndicator ?? throw new ArgumentNullException(nameof(loadingIndicator));
		_api = api ?? throw new ArgumentNullException(nameof(api));
		_windowShellProvider = windowShellProvider ?? throw new ArgumentNullException(nameof(windowShellProvider));
		_appConfig = appConfig;
		_imageVariantsLogger = imageVariantsLogger;

		_debounceTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
		_debounceTimer.Interval = TimeSpan.FromMilliseconds(300);
		_debounceTimer.Tick += async (s, e) =>
		{
			_debounceTimer.Stop();
			await SearchAsync();
		};
	}

	public override string Title => Localizer.Instance.GetString("Media");



	[ObservableProperty]
	public partial ObservableCollection<StorageItemInfoViewModel> MediaFiles { get; set; } = new();

	[ObservableProperty]
	public partial string SearchFilter { get; set; } = "";

	partial void OnSearchFilterChanged(string value)
	{
		_debounceTimer.Stop();
		_debounceTimer.Start();
	}

	[ObservableProperty]
	public partial MediaFilterMode FilterMode { get; set; } = MediaFilterMode.All;

	[ObservableProperty]
	public partial bool HasMoreItems { get; set; } = true;

	[ObservableProperty]
	public partial bool IsLoadingMore { get; set; }

	public StorageItemInfoViewModel[] FilteredFiles => MediaFiles.ToArray();

	partial void OnFilterModeChanged(MediaFilterMode value)
	{
		_ = RefreshListAsync();
	}

	public override async void ViewLoaded()
	{
		await RefreshListAsync();
	}

	[RelayCommand]
	private async Task SearchAsync()
	{
		await RefreshListAsync();
	}

	private BlobKindFilter? GetBlobKindFilter()
	{
		return FilterMode switch
		{
			MediaFilterMode.Images => BlobKindFilter.Images,
			MediaFilterMode.Files => BlobKindFilter.Files,
			_ => null
		};
	}

	private async Task RefreshListAsync()
	{
		using var loadingScope = _loadingIndicator.BeginLoading();
		try
		{
			_currentPage = 1;
			_hasMoreItems = true;

			var search = string.IsNullOrWhiteSpace(SearchFilter) ? null : SearchFilter;
			var response = await _api.GetMediaAsync(_currentPage, PageSize, GetBlobKindFilter(), search);
			await response.EnsureSuccessfulAsync();

			MediaFiles.Clear();

			if (response.IsSuccessStatusCode && response.Content != null)
			{
				MediaFiles.AddRange(response.Content.Data.Select(i => new StorageItemInfoViewModel(i, _appConfig.Value)));
				_hasMoreItems = response.Content.PageNumber * response.Content.PageSize < response.Content.TotalCount;
			}

			HasMoreItems = _hasMoreItems;
			OnPropertyChanged(nameof(FilteredFiles));
		}
		catch (Exception ex)
		{
			await _dialogService.ShowStatusMessageAsync(
				StatusMessageDialogType.Error,
				Localizer.Instance.GetString("CouldNotLoadData"),
				$"{Localizer.Instance.GetString("ErrorLoadingData")} {ex}");
		}
	}

	[RelayCommand]
	private async Task LoadMoreAsync()
	{
		if (IsLoadingMore || !HasMoreItems)
		{
			return;
		}

		IsLoadingMore = true;
		try
		{
			_currentPage++;
			var search = string.IsNullOrWhiteSpace(SearchFilter) ? null : SearchFilter;
			var response = await _api.GetMediaAsync(_currentPage, PageSize, GetBlobKindFilter(), search);
			await response.EnsureSuccessfulAsync();

			if (response.Content != null)
			{
				MediaFiles.AddRange(response.Content.Data.Select(i => new StorageItemInfoViewModel(i, _appConfig.Value)));
				_hasMoreItems = response.Content.PageNumber * response.Content.PageSize < response.Content.TotalCount;
			}

			HasMoreItems = _hasMoreItems;
			OnPropertyChanged(nameof(FilteredFiles));
		}
		catch (Exception ex)
		{
			await _dialogService.ShowStatusMessageAsync(
				StatusMessageDialogType.Error,
				Localizer.Instance.GetString("CouldNotLoadData"),
				$"{Localizer.Instance.GetString("ErrorLoadingData")} {ex}");
		}
		finally
		{
			IsLoadingMore = false;
		}
	}

	[RelayCommand]
	private async Task UploadFileAsync()
	{
		try
		{
			var picker = new FileOpenPicker();
			picker.ViewMode = PickerViewMode.Thumbnail;
			picker.FileTypeFilter.Add("*");

			// Initialize the file picker with the window handle
			var window = _windowShellProvider.Window;
			var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
			WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

			var file = await picker.PickSingleFileAsync();
			if (file != null)
			{
				using var loadingScope = _loadingIndicator.BeginLoading();
				try
				{
					using var stream = await file.OpenStreamForReadAsync();
					var streamPart = new StreamPart(stream, file.Name, file.ContentType);

					var isImage = IsImageFile(file.Name);
					var response = isImage
						? await _api.UploadImageAsync(streamPart, file.Name)
						: await _api.UploadFileAsync(streamPart, file.Name);

					await response.EnsureSuccessfulAsync();
					await RefreshListAsync();
				}
				catch (Exception ex)
				{
					await _dialogService.ShowStatusMessageAsync(
						StatusMessageDialogType.Error,
						Localizer.Instance.GetString("UploadFailed"),
						$"{Localizer.Instance.GetString("ErrorUploadingFile")} {ex}");
				}
			}
		}
		catch (Exception ex)
		{
			await _dialogService.ShowStatusMessageAsync(
				StatusMessageDialogType.Error,
				Localizer.Instance.GetString("UploadFailed"),
				$"{Localizer.Instance.GetString("ErrorOpeningFilePicker")} {ex}");
		}
	}

	[RelayCommand]
	private async Task DeleteFileAsync(StorageItemInfoViewModel? file)
	{
		if (file == null)
		{
			return;
		}

		// Show confirmation dialog
		var confirmResult = await _dialogService.ShowConfirmationDialogAsync(
			Localizer.Instance.GetString("DeleteFile"),
			Localizer.Instance.GetString("ConfirmDeleteFile"));

		if (confirmResult != ContentDialogResult.Primary)
		{
			return;
		}

		using var loadingScope = _loadingIndicator.BeginLoading();
		try
		{
			var isImage = IsImageFile(file.FileName);
			var response = isImage
				? await _api.DeleteImageAsync(file.BlobPath)
				: await _api.DeleteFileAsync(file.BlobPath);
			response.EnsureSuccessStatusCode();
			await RefreshListAsync();
		}
		catch (Exception ex)
		{
			await _dialogService.ShowStatusMessageAsync(
				StatusMessageDialogType.Error,
				Localizer.Instance.GetString("DeleteFailed"),
				$"{Localizer.Instance.GetString("ErrorDeletingFile")} {ex}");
		}
	}

	[RelayCommand]
	private async Task ShowVariantsAsync(StorageItemInfoViewModel? file)
	{
		if (file == null || !IsImageFile(file.FileName))
		{
			return;
		}

		var viewModel = new ImageVariantsDialogViewModel(_api, _imageVariantsLogger, file);
		await _dialogService.ShowAsync(viewModel);
	}

	private static bool IsImageFile(string fileName)
	{
		var extension = Path.GetExtension(fileName).ToLowerInvariant();
		return extension is ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".bmp" or ".svg";
	}
}

public enum MediaFilterMode
{
	All,
	Images,
	Files
}
