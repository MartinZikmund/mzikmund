using System.Collections.ObjectModel;
using MZikmund.Api.Client;
using MZikmund.DataContracts.Blog;
using MZikmund.Models.Dialogs;
using MZikmund.Services.Dialogs;
using MZikmund.Services.Loading;
using MZikmund.Services.Localization;
using Refit;

namespace MZikmund.ViewModels.Admin;

public partial class TagsManagerViewModel : PageViewModel
{
	private readonly IDialogService _dialogService;
	private readonly ILoadingIndicator _loadingIndicator;
	private readonly IMZikmundApi _api;
	private readonly ILogger<TagsManagerViewModel> _logger;
	private Tag[] _allTags = Array.Empty<Tag>();

	public TagsManagerViewModel(
		IMZikmundApi api,
		IDialogService dialogService,
		ILoadingIndicator loadingIndicator,
		ILogger<TagsManagerViewModel> logger)
	{
		_dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
		_loadingIndicator = loadingIndicator ?? throw new ArgumentNullException(nameof(loadingIndicator));
		_api = api ?? throw new ArgumentNullException(nameof(api));
		_logger = logger;
	}

	public override string Title => Localizer.Instance.GetString("Tags");

	public ObservableCollection<Tag> Tags { get; } = new();

	[ObservableProperty]
	public partial string SearchText { get; set; } = "";

	[ObservableProperty]
	public partial bool IsLoading { get; set; }

	[ObservableProperty]
	public partial bool HasError { get; set; }

	public bool HasItems => Tags.Count > 0;

	public string CountText => string.Format(System.Globalization.CultureInfo.CurrentCulture, Localizer.Instance.GetString("TagCountFormat"), _allTags.Length);

	partial void OnSearchTextChanged(string value) => ApplyFilter();

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
			var tags = await _api.GetTagsAsync();
			await tags.EnsureSuccessfulAsync();
			_allTags = tags.Content!.OrderBy(t => t.DisplayName, StringComparer.CurrentCultureIgnoreCase).ToArray();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Loading tags failed");
			HasError = true;
		}
		finally
		{
			IsLoading = false;
			ApplyFilter();
			OnPropertyChanged(nameof(CountText));
		}
	}

	private void ApplyFilter()
	{
		var query = SearchText.Trim();
		Tags.Clear();
		foreach (var tag in _allTags.Where(t => query.Length == 0 || t.DisplayName.Contains(query, StringComparison.CurrentCultureIgnoreCase)))
		{
			Tags.Add(tag);
		}

		OnPropertyChanged(nameof(HasItems));
	}

	[RelayCommand]
	private async Task AddTagAsync()
	{
		var viewModel = new AddOrUpdateTagDialogViewModel();
		if (await _dialogService.ShowAsync(viewModel) != ContentDialogResult.Primary)
		{
			return;
		}

		await RunAndRefreshAsync(() => _api.AddTagAsync(new Tag()
		{
			DisplayName = viewModel.Tag.DisplayName,
			RouteName = viewModel.Tag.RouteName,
		}));
	}

	[RelayCommand]
	private async Task UpdateTagAsync(Tag? tag)
	{
		if (tag is null)
		{
			return;
		}

		var viewModel = new AddOrUpdateTagDialogViewModel(new TagViewModel()
		{
			Id = tag.Id,
			DisplayName = tag.DisplayName,
			RouteName = tag.RouteName
		});

		var result = await _dialogService.ShowAsync(viewModel);
		if (result == ContentDialogResult.Primary)
		{
			await RunAndRefreshAsync(() => _api.UpdateTagAsync(tag.Id, new Tag()
			{
				DisplayName = viewModel.Tag.DisplayName,
				RouteName = viewModel.Tag.RouteName,
			}));
		}
		else if (result == ContentDialogResult.Secondary)
		{
			await RunAndRefreshAsync(() => _api.DeleteTagAsync(tag.Id));
		}
	}

	private Task RunAndRefreshAsync<T>(Func<Task<ApiResponse<T>>> action) =>
		RunAndRefreshAsync(async () => (IApiResponse)await action());

	private async Task RunAndRefreshAsync(Func<Task<IApiResponse>> action)
	{
		using (_loadingIndicator.BeginLoading())
		{
			try
			{
				var response = await action();
				if (!response.IsSuccessStatusCode)
				{
					throw (Exception?)response.Error ?? new InvalidOperationException(response.StatusCode.ToString());
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Saving tag failed");
				await _dialogService.ShowStatusMessageAsync(
					StatusMessageDialogType.Error,
					Localizer.Instance.GetString("SaveFailed"),
					ex.Message);
			}
		}

		await RefreshAsync();
	}
}
