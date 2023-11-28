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
		await RefreshListAsync();
	}

	private async Task RefreshListAsync()
	{
		using var loadingScope = _loadingIndicator.BeginLoading();
		try
		{
			//TODO: Refresh collection based on IDs
			var categories = await _api.GetCategoriesAsync();
			Categories.Clear();
			Categories.AddRange(categories.Content!.OrderBy(t => t.DisplayName));
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

	public ICommand UpdateCategoryCommand => GetOrCreateAsyncCommand<Category>(UpdateCategoryAsync);

	private async Task AddCategoryAsync()
	{
		var viewModel = new AddOrUpdateCategoryDialogViewModel();
		var result = await _dialogService.ShowAsync(viewModel);
		if (result != ContentDialogResult.Primary)
		{
			return;
		}

		using var loadingScope = _loadingIndicator.BeginLoading();
		var apiResponse = await _api.AddCategoryAsync(new Category()
		{
			DisplayName = viewModel.Category.DisplayName,
			RouteName = viewModel.Category.RouteName,
		});

		await RefreshListAsync();
	}

	private async Task UpdateCategoryAsync(Category? category)
	{
		if (category is null)
		{
			return;
		}

		var viewModel = new AddOrUpdateCategoryDialogViewModel(new CategoryViewModel()
		{
			Id = category.Id,
			DisplayName = category.DisplayName,
			RouteName = category.RouteName
		});
		var result = await _dialogService.ShowAsync(viewModel);

		using var loadingScope = _loadingIndicator.BeginLoading();
		if (result == ContentDialogResult.Primary)
		{
			var apiResponse = await _api.UpdateCategoryAsync(category.Id, new EditCategory()
			{
				DisplayName = viewModel.Category.DisplayName,
				RouteName = viewModel.Category.RouteName,
			});
		}
		else if (result == ContentDialogResult.Secondary)
		{
			var apiResponse = await _api.DeleteCategoryAsync(category.Id);
		}

		await RefreshListAsync();
	}
}
