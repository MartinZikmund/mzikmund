using System.Collections.ObjectModel;
using MZikmund.Api.Client;
using MZikmund.DataContracts.Blog;
using MZikmund.Extensions;
using MZikmund.Models.Dialogs;
using MZikmund.Services.Dialogs;
using MZikmund.Services.Loading;
using MZikmund.Services.Localization;
using Newtonsoft.Json;
using Windows.Storage.Pickers;

namespace MZikmund.ViewModels.Admin;

public class CategoriesManagerViewModel : PageViewModel
{
	private readonly IDialogService _dialogService;
	private readonly ILoadingIndicator _loadingIndicator;
	private readonly IMZikmundApi _api;

	public CategoriesManagerViewModel(
		IMZikmundApi api,
		IDialogService dialogService,
		ILoadingIndicator loadingIndicator)
	{
		_dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
		_loadingIndicator = loadingIndicator ?? throw new ArgumentNullException(nameof(loadingIndicator));
		_api = api ?? throw new ArgumentNullException(nameof(api));
	}

	public override string Title => Localizer.Instance.GetString("Categories");

	public ObservableCollection<Category> Categories { get; } = new ObservableCollection<Category>();

	public override async void ViewAppeared()
	{
		using var loadingScope = _loadingIndicator.BeginLoading();
		try
		{
			//TODO: Refresh collection based on IDs
			var categories = await _api.GetCategoriesAsync();
			Categories.AddRange(categories.Content!);
		}
		catch (Exception ex)
		{
			await _dialogService.ShowStatusMessageAsync(
				StatusMessageDialogType.Error,
				"Could not load data",
				$"Error occurred loading data from server. {ex}");
		}
	}

	public ICommand AddCategoryCommand => GetOrCreateAsyncCommand(AddCategoryAsync);

	public ICommand ImportJsonCommand => GetOrCreateAsyncCommand(ImportJsonAsync);

	private async Task ImportJsonAsync()
	{
		using var loadingScope = _loadingIndicator.BeginLoading();
		var picker = new FileOpenPicker();
		picker.FileTypeFilter.Add(".json");
		var jsonFile = await picker.PickSingleFileAsync();
		var jsonContent = await FileIO.ReadTextAsync(jsonFile);
		var Categories = JsonConvert.DeserializeObject<Category[]>(jsonContent);
		if (Categories == null)
		{
			return;
		}

		for (int i = 0; i < Categories.Length; i++)
		{
			var tag = Categories[i];
			_loadingIndicator.StatusMessage = $"Adding tag {i + 1} of {Categories.Length}";
			// Ensure tag ID is empty.
			tag.Id = Guid.Empty;

			await _api.AddCategoryAsync(tag);
		}
	}

	private async Task AddCategoryAsync()
	{
		var viewModel = new AddOrUpdateCategoryDialogViewModel();
		var result = await _dialogService.ShowAsync(viewModel);
		if (result != ContentDialogResult.Primary)
		{
			return;
		}

		//var apiResponse = await _api.AddCategoryAsync(new CategoryDto()
		//{
		//	Localizations = new[]
		//	{
		//		new CategoryLocalizationDto()
		//		{
		//			DisplayName = "test",
		//			LanguageId = 1,
		//			RouteName = "test"
		//		}
		//	}
		//});
	}
}
