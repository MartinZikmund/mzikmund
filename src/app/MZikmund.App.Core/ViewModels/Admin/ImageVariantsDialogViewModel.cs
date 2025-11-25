using MZikmund.Api.Client;
using MZikmund.DataContracts.Blobs;
using MZikmund.ViewModels.Items;
using Windows.ApplicationModel.DataTransfer;

namespace MZikmund.ViewModels.Admin;

public partial class ImageVariantsDialogViewModel : DialogViewModel
{
	private readonly IMZikmundApi _api;
	private readonly StorageItemInfoViewModel _selectedFile;

	public ImageVariantsDialogViewModel(
		IMZikmundApi api,
		StorageItemInfoViewModel selectedFile)
	{
		_api = api;
		_selectedFile = selectedFile;
	}

	public string FileName => _selectedFile.FileName;

	[ObservableProperty]
	public partial List<ImageVariant> AvailableVariants { get; set; } = new List<ImageVariant>();

	[ObservableProperty]
	public partial ImageVariant? SelectedVariant { get; set; }

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
				AvailableVariants = new List<ImageVariant>();
				SelectedVariant = null;
			}
		}
		catch (Exception)
		{
			AvailableVariants = new List<ImageVariant>();
			SelectedVariant = null;
		}
		finally
		{
			IsLoading = false;
		}
	}
}
