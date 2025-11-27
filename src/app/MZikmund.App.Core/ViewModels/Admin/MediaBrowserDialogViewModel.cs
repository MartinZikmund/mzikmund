using MZikmund.Api.Client;
using Refit;
using MZikmund.DataContracts.Blobs;
using MZikmund.Services.Navigation;
using Microsoft.Extensions.Options;
using MZikmund.Business.Models;
using MZikmund.ViewModels.Items;

namespace MZikmund.ViewModels.Admin;

public partial class MediaBrowserDialogViewModel : DialogViewModel
{
	private readonly IMZikmundApi _api;
	private readonly IWindowShellProvider _windowShellProvider;
	private readonly IOptions<AppConfig> _appConfig;
	private readonly bool _isImageMode;

	public MediaBrowserDialogViewModel(
		IMZikmundApi api,
		IWindowShellProvider windowShellProvider,
		IOptions<AppConfig> appConfig,
		bool isImageMode = true)
	{
		_api = api;
		_windowShellProvider = windowShellProvider;
		_appConfig = appConfig;
		_isImageMode = isImageMode;
	}

	private const int PageSize = 50;
	private int _currentPage = 1;
	private bool _hasMoreItems = true;

	[ObservableProperty]
	public partial List<StorageItemInfoViewModel> MediaFiles { get; set; } = new List<StorageItemInfoViewModel>();

	[ObservableProperty]
	public partial StorageItemInfoViewModel? SelectedFile { get; set; }

	[ObservableProperty]
	public partial ImageVariant[] AvailableVariants { get; set; } = Array.Empty<ImageVariant>();

	[ObservableProperty]
	public partial ImageVariant? SelectedVariant { get; set; }

	public Uri? SelectedUrl => _isImageMode && SelectedVariant != null
		? SelectedVariant.Url
		: SelectedFile?.Url;

	partial void OnSelectedFileChanged(StorageItemInfoViewModel? value)
	{
		if (value != null && _isImageMode)
		{
			// Load available variants from API
			_ = LoadImageVariantsAsync(value.BlobPath);
		}
		else
		{
			AvailableVariants = Array.Empty<ImageVariant>();
			SelectedVariant = null;
		}
		OnPropertyChanged(nameof(SelectedUrl));
	}

	private async Task LoadImageVariantsAsync(string imagePath)
	{
		try
		{
			var response = await _api.GetImageVariantsAsync(imagePath);
			if (response.IsSuccessStatusCode && response.Content != null)
			{
				AvailableVariants = response.Content;
				SelectedVariant = AvailableVariants.FirstOrDefault(); // Select original by default
			}
			else
			{
				// Fallback to empty list if API call fails
				AvailableVariants = Array.Empty<ImageVariant>();
				SelectedVariant = null;
			}
		}
		catch (Exception)
		{
			// Fallback to empty list on error
			AvailableVariants = Array.Empty<ImageVariant>();
			SelectedVariant = null;
		}
	}

	partial void OnSelectedVariantChanged(ImageVariant? value)
	{
		OnPropertyChanged(nameof(SelectedUrl));
	}

	[ObservableProperty]
	public partial string SearchFilter { get; set; } = "";

	[ObservableProperty]
	public partial bool IsLoading { get; set; }

	[ObservableProperty]
	public partial bool HasMoreItems { get; set; } = true;

	[ObservableProperty]
	public partial bool IsLoadingMore { get; set; }

	public List<StorageItemInfoViewModel> FilteredFiles => MediaFiles;

	private BlobKindFilter? GetBlobKindFilter()
	{
		return _isImageMode ? BlobKindFilter.Images : BlobKindFilter.Files;
	}

	[RelayCommand]
	private async Task LoadFilesAsync()
	{
		IsLoading = true;
		try
		{
			_currentPage = 1;
			_hasMoreItems = true;

			var search = string.IsNullOrWhiteSpace(SearchFilter) ? null : SearchFilter;
			var response = await _api.GetMediaAsync(_currentPage, PageSize, GetBlobKindFilter(), search);
			await response.EnsureSuccessfulAsync();
			if (response.IsSuccessStatusCode && response.Content != null)
			{
				MediaFiles = response.Content.Data.Select(i => new StorageItemInfoViewModel(i, _appConfig.Value)).ToList();
				_hasMoreItems = response.Content.PageNumber * response.Content.PageSize < response.Content.TotalCount;
				HasMoreItems = _hasMoreItems;
				OnPropertyChanged(nameof(FilteredFiles));
			}
		}
		catch (Exception)
		{
			// TODO: Handle error
		}
		finally
		{
			IsLoading = false;
		}
	}

	[RelayCommand]
	private async Task SearchAsync()
	{
		await LoadFilesAsync();
	}

	[RelayCommand]
	private async Task LoadMoreAsync()
	{
		if (IsLoadingMore || !_hasMoreItems)
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
			if (response.IsSuccessStatusCode && response.Content != null)
			{
				MediaFiles.AddRange(response.Content.Data);
				_hasMoreItems = response.Content.PageNumber * response.Content.PageSize < response.Content.TotalCount;
				HasMoreItems = _hasMoreItems;
				OnPropertyChanged(nameof(FilteredFiles));
			}
		}
		catch (Exception)
		{
			// TODO: Handle error
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
			var picker = new Windows.Storage.Pickers.FileOpenPicker();
			picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;

			if (_isImageMode)
			{
				picker.FileTypeFilter.Add(".jpg");
				picker.FileTypeFilter.Add(".jpeg");
				picker.FileTypeFilter.Add(".png");
				picker.FileTypeFilter.Add(".gif");
				picker.FileTypeFilter.Add(".webp");
			}
			else
			{
				picker.FileTypeFilter.Add("*");
			}

			// Initialize the file picker with the window handle
			var window = _windowShellProvider.Window;
			var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
			WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

			var file = await picker.PickSingleFileAsync();
			if (file != null)
			{
				IsLoading = true;
				try
				{
					using var stream = await file.OpenStreamForReadAsync();
					var streamPart = new StreamPart(stream, file.Name, file.ContentType);

					var response = _isImageMode
						? await _api.UploadImageAsync(streamPart, file.Name)
						: await _api.UploadFileAsync(streamPart, file.Name);
					await response.EnsureSuccessfulAsync();
					if (response.IsSuccessStatusCode)
					{
						await LoadFilesAsync();
					}
				}
				finally
				{
					IsLoading = false;
				}
			}
		}
		catch (Exception)
		{
			// TODO: Handle error
		}
	}

	public override async void OnOpened(ContentDialog contentDialog)
	{
		await LoadFilesAsync();
	}
}
