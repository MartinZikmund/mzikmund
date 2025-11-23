using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MZikmund.Api.Client;
using MZikmund.DataContracts.Blobs;
using MZikmund.Extensions;
using MZikmund.Models.Dialogs;
using MZikmund.Services.Dialogs;
using MZikmund.Services.Loading;
using MZikmund.Services.Localization;
using MZikmund.Services.Navigation;
using Refit;
using Windows.Storage.Pickers;

namespace MZikmund.ViewModels.Admin;

public partial class MediaBrowserViewModel : PageViewModel
{
	private readonly IDialogService _dialogService;
	private readonly ILoadingIndicator _loadingIndicator;
	private readonly IMZikmundApi _api;
	private readonly IWindowShellProvider _windowShellProvider;

	public MediaBrowserViewModel(
		IMZikmundApi api,
		IDialogService dialogService,
		ILoadingIndicator loadingIndicator,
		IWindowShellProvider windowShellProvider)
	{
		_dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
		_loadingIndicator = loadingIndicator ?? throw new ArgumentNullException(nameof(loadingIndicator));
		_api = api ?? throw new ArgumentNullException(nameof(api));
		_windowShellProvider = windowShellProvider ?? throw new ArgumentNullException(nameof(windowShellProvider));
	}

	public override string Title => Localizer.Instance.GetString("Media");

	[ObservableProperty]
	public partial ObservableCollection<StorageItemInfo> MediaFiles { get; set; } = new ObservableCollection<StorageItemInfo>();

	[ObservableProperty]
	public partial string SearchFilter { get; set; } = "";

	[ObservableProperty]
	public partial MediaFilterMode FilterMode { get; set; } = MediaFilterMode.All;

	public StorageItemInfo[] FilteredFiles
	{
		get
		{
			var files = MediaFiles.AsEnumerable();

			// Apply filter mode
			if (FilterMode == MediaFilterMode.Images)
			{
				files = files.Where(f => IsImageFile(f.FileName));
			}
			else if (FilterMode == MediaFilterMode.Files)
			{
				files = files.Where(f => !IsImageFile(f.FileName));
			}

			// Apply search filter
			if (!string.IsNullOrEmpty(SearchFilter))
			{
				files = files.Where(f => f.FileName.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase));
			}

			return files.ToArray();
		}
	}

	partial void OnSearchFilterChanged(string value)
	{
		OnPropertyChanged(nameof(FilteredFiles));
	}

	partial void OnFilterModeChanged(MediaFilterMode value)
	{
		OnPropertyChanged(nameof(FilteredFiles));
	}

	public override async void ViewLoaded()
	{
		await RefreshListAsync();
	}

	private async Task RefreshListAsync()
	{
		using var loadingScope = _loadingIndicator.BeginLoading();
		try
		{
			var imagesResponse = await _api.GetImagesAsync();
			var filesResponse = await _api.GetFilesAsync();

			MediaFiles.Clear();

			if (imagesResponse.IsSuccessStatusCode && imagesResponse.Content != null)
			{
				MediaFiles.AddRange(imagesResponse.Content);
			}

			if (filesResponse.IsSuccessStatusCode && filesResponse.Content != null)
			{
				MediaFiles.AddRange(filesResponse.Content);
			}

			OnPropertyChanged(nameof(FilteredFiles));
		}
		catch (Exception ex)
		{
			await _dialogService.ShowStatusMessageAsync(
				StatusMessageDialogType.Error,
				"Could not load data",
				$"Error occurred loading data from server. {ex}");
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

					if (response.IsSuccessStatusCode)
					{
						await RefreshListAsync();
					}
				}
				catch (Exception ex)
				{
					await _dialogService.ShowStatusMessageAsync(
						StatusMessageDialogType.Error,
						"Upload failed",
						$"Error occurred uploading file. {ex}");
				}
			}
		}
		catch (Exception ex)
		{
			await _dialogService.ShowStatusMessageAsync(
				StatusMessageDialogType.Error,
				"Upload failed",
				$"Error occurred opening file picker. {ex}");
		}
	}

	[RelayCommand]
	private async Task DeleteFileAsync(StorageItemInfo? file)
	{
		if (file == null)
		{
			return;
		}

		// Show confirmation dialog
		var confirmResult = await _dialogService.ShowStatusMessageAsync(
			StatusMessageDialogType.Warning,
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
				? await _api.DeleteImageAsync(file.FileName)
				: await _api.DeleteFileAsync(file.FileName);

			if (response.IsSuccessStatusCode)
			{
				await RefreshListAsync();
			}
		}
		catch (Exception ex)
		{
			await _dialogService.ShowStatusMessageAsync(
				StatusMessageDialogType.Error,
				"Delete failed",
				$"Error occurred deleting file. {ex}");
		}
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
