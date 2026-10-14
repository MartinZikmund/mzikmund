using System.Collections.ObjectModel;
using MZikmund.Api.Client;
using MZikmund.DataContracts.Blog;
using MZikmund.Extensions;
using MZikmund.Models.Dialogs;
using MZikmund.Services.Dialogs;
using MZikmund.Services.Loading;
using MZikmund.Services.Localization;
using Refit;

namespace MZikmund.ViewModels.Admin;

public partial class CategoriesManagerViewModel : PageViewModel
{
	private readonly IDialogService _dialogService;
	private readonly ILoadingIndicator _loadingIndicator;
	private readonly IMZikmundApi _api;
	private readonly ILogger<CategoriesManagerViewModel> _logger;

	public CategoriesManagerViewModel(
		IMZikmundApi api,
		IDialogService dialogService,
		ILoadingIndicator loadingIndicator,
		ILogger<CategoriesManagerViewModel> logger)
	{
		_dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
		_loadingIndicator = loadingIndicator ?? throw new ArgumentNullException(nameof(loadingIndicator));
		_api = api ?? throw new ArgumentNullException(nameof(api));
		_logger = logger;
	}

	public override string Title => Localizer.Instance.GetString("Categories");

	public ObservableCollection<Category> Categories { get; } = new();

	[ObservableProperty]
	public partial bool IsLoading { get; set; }

	[ObservableProperty]
	public partial bool HasError { get; set; }

	public bool HasItems => Categories.Count > 0;

	public override async void ViewLoaded()
	{
		base.ViewLoaded();
		await RefreshAsync();
	}

	[RelayCommand]
	private async Task RefreshAsync()
	{
		IsLoading = true;
		HasError = false;
		try
		{
			var categories = await _api.GetCategoriesAsync();
			await categories.EnsureSuccessfulAsync();
			Categories.Clear();
			Categories.AddRange(categories.Content!.OrderBy(t => t.DisplayName));
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Loading categories failed");
			HasError = true;
		}
		finally
		{
			IsLoading = false;
			OnPropertyChanged(nameof(HasItems));
		}
	}

	[RelayCommand]
	private async Task AddCategoryAsync()
	{
		var viewModel = new AddOrUpdateCategoryDialogViewModel();
		if (await _dialogService.ShowAsync(viewModel) != ContentDialogResult.Primary)
		{
			return;
		}

		await RunAndRefreshAsync(() => _api.AddCategoryAsync(new Category()
		{
			DisplayName = viewModel.Category.DisplayName,
			RouteName = viewModel.Category.RouteName,
		}));
	}

	[RelayCommand]
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
		if (result == ContentDialogResult.Primary)
		{
			await RunAndRefreshAsync(() => _api.UpdateCategoryAsync(category.Id, new Category()
			{
				DisplayName = viewModel.Category.DisplayName,
				RouteName = viewModel.Category.RouteName,
			}));
		}
		else if (result == ContentDialogResult.Secondary)
		{
			await RunAndRefreshAsync(() => _api.DeleteCategoryAsync(category.Id));
		}
	}

	private async Task RunAndRefreshAsync<T>(Func<Task<ApiResponse<T>>> action)
	{
		using (_loadingIndicator.BeginLoading())
		{
			try
			{
				var response = await action();
				await response.EnsureSuccessfulAsync();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Saving category failed");
				await _dialogService.ShowStatusMessageAsync(
					StatusMessageDialogType.Error,
					Localizer.Instance.GetString("SaveFailed"),
					ex.Message);
			}
		}

		await RefreshAsync();
	}
}
