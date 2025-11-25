using MZikmund.Api.Client;
using MZikmund.App.Core.ViewModels.Admin;
using MZikmund.Services.Dialogs;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using MZikmund.App;
using MZikmund.ViewModels;
using Refit;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MZikmund.DataContracts.Blobs;
using MZikmund.Services.Navigation;

namespace MZikmund.ViewModels.Admin;

public partial class MediaBrowserDialogViewModel : DialogViewModel
{
	private readonly IMZikmundApi _api;
	private readonly IWindowShellProvider _windowShellProvider;
	private readonly bool _isImageMode;

	public MediaBrowserDialogViewModel(
		IMZikmundApi api,
		IWindowShellProvider windowShellProvider,
		bool isImageMode = true)
	{
		_api = api;
		_windowShellProvider = windowShellProvider;
		_isImageMode = isImageMode;
	}

	private const int PageSize = 50;
	private int _currentPage = 1;
	private bool _hasMoreItems = true;

	[ObservableProperty]
	public partial List<StorageItemInfo> MediaFiles { get; set; } = new List<StorageItemInfo>();

	[ObservableProperty]
	public partial StorageItemInfo? SelectedFile { get; set; }

	[ObservableProperty]
	public partial List<ImageVariant> AvailableVariants { get; set; } = new List<ImageVariant>();

	[ObservableProperty]
	public partial ImageVariant? SelectedVariant { get; set; }

	public string? SelectedUrl => _isImageMode && SelectedVariant != null 
		? SelectedVariant.Url 
		: (SelectedFile != null ? GetPublicUrl(SelectedFile.BlobPath) : null);

	partial void OnSelectedFileChanged(StorageItemInfo? value)
	{
		if (value != null && _isImageMode)
		{
			// Load available variants from API
			_ = LoadImageVariantsAsync(value.BlobPath);
		}
		else
		{
			AvailableVariants = new List<ImageVariant>();
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
				AvailableVariants = new List<ImageVariant>();
				SelectedVariant = null;
			}
		}
		catch (Exception)
		{
			// Fallback to empty list on error
			AvailableVariants = new List<ImageVariant>();
			SelectedVariant = null;
		}
	}

	partial void OnSelectedVariantChanged(ImageVariant? value)
	{
		OnPropertyChanged(nameof(SelectedUrl));
	}

	private string GetPublicUrl(string blobPath)
	{
		return $"https://mzikmund.blob.core.windows.net/media/{blobPath}";
	}

	[ObservableProperty]
	public partial string SearchFilter { get; set; } = "";

	[ObservableProperty]
	public partial bool IsLoading { get; set; }

	[ObservableProperty]
	public partial bool HasMoreItems { get; set; } = true;

	[ObservableProperty]
	public partial bool IsLoadingMore { get; set; }

	public List<StorageItemInfo> FilteredFiles => MediaFiles;

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

			if (response.IsSuccessStatusCode && response.Content != null)
			{
				MediaFiles = response.Content.Data.ToList();
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
