using MZikmund.Api.Client;
using MZikmund.App.Core.ViewModels.Admin;
using MZikmund.Web.Core.Services.Blobs;
using MZikmund.Services.Dialogs;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using MZikmund.App;
using MZikmund.ViewModels;
using Refit;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MZikmund.ViewModels.Admin;

public partial class MediaBrowserDialogViewModel : DialogViewModel
{
	private readonly IMZikmundApi _api;
	private readonly bool _isImageMode;

	public MediaBrowserDialogViewModel(IMZikmundApi api, bool isImageMode = true)
	{
		_api = api;
		_isImageMode = isImageMode;
	}

	[ObservableProperty]
	public partial BlobInfo[] MediaFiles { get; set; } = Array.Empty<BlobInfo>();

	[ObservableProperty]
	public partial BlobInfo? SelectedFile { get; set; }

	[ObservableProperty]
	public partial string SearchFilter { get; set; } = "";

	[ObservableProperty]
	public partial bool IsLoading { get; set; }

	public BlobInfo[] FilteredFiles => string.IsNullOrEmpty(SearchFilter) 
		? MediaFiles 
		: MediaFiles.Where(f => f.FileName.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase)).ToArray();

	partial void OnSearchFilterChanged(string value)
	{
		OnPropertyChanged(nameof(FilteredFiles));
	}

	[RelayCommand]
	private async Task LoadFilesAsync()
	{
		IsLoading = true;
		try
		{
			var response = _isImageMode 
				? await _api.GetImagesAsync() 
				: await _api.GetFilesAsync();
			
			if (response.IsSuccessStatusCode)
			{
				MediaFiles = response.Content?.ToArray() ?? Array.Empty<BlobInfo>();
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
			var app = (MZikmundApp)Application.Current;
			var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(app.MainWindow!);
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