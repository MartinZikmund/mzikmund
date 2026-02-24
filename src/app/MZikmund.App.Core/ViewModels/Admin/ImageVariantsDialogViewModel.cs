using Microsoft.Extensions.Logging;
using MZikmund.Api.Client;
using MZikmund.DataContracts.Blobs;
using MZikmund.ViewModels.Items;
using Windows.ApplicationModel.DataTransfer;

namespace MZikmund.ViewModels.Admin;

public partial class ImageVariantsDialogViewModel : DialogViewModel
{
	private readonly IMZikmundApi _api;
	private readonly ILogger<ImageVariantsDialogViewModel> _logger;
	private readonly StorageItemInfoViewModel _selectedFile;

	public ImageVariantsDialogViewModel(
		IMZikmundApi api,
		ILogger<ImageVariantsDialogViewModel> logger,
		StorageItemInfoViewModel selectedFile)
	{
		_api = api;
		_logger = logger;
		_selectedFile = selectedFile;
	}

	public string FileName => _selectedFile.FileName;

	[ObservableProperty]
	public partial ImageVariant[] AvailableVariants { get; set; } = Array.Empty<ImageVariant>();

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(SelectedVariantUrl))]
	public partial ImageVariant? SelectedVariant { get; set; }

	public Uri? SelectedVariantUrl => SelectedVariant?.Url;

	[ObservableProperty]
	public partial bool IsLoading { get; set; }

	[RelayCommand]
	private void CopyUrl()
	{
		if (SelectedVariant != null)
		{
			var dataPackage = new DataPackage();
			dataPackage.SetText(SelectedVariant.Url.ToString());
			Clipboard.SetContent(dataPackage);
		}
	}

	public override async void OnOpened(ContentDialog contentDialog)
	{
		await LoadImageVariantsAsync();
	}

	private async Task LoadImageVariantsAsync()
	{
		IsLoading = true;
		try
		{
			var response = await _api.GetImageVariantsAsync(_selectedFile.BlobPath);
			if (response.IsSuccessStatusCode && response.Content != null)
			{
				AvailableVariants = response.Content;
				SelectedVariant = AvailableVariants.FirstOrDefault();
			}
			else
			{
				AvailableVariants = Array.Empty<ImageVariant>();
				SelectedVariant = null;
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to load image variants for {BlobPath}", _selectedFile.BlobPath);
			AvailableVariants = Array.Empty<ImageVariant>();
			SelectedVariant = null;
		}
		finally
		{
			IsLoading = false;
		}
	}
}
