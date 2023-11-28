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

public class TagsManagerViewModel : PageViewModel
{
	private readonly IDialogService _dialogService;
	private readonly ILoadingIndicator _loadingIndicator;
	private readonly IMZikmundApi _api;

	public TagsManagerViewModel(
		IMZikmundApi api,
		IDialogService dialogService,
		ILoadingIndicator loadingIndicator)
	{
		_dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
		_loadingIndicator = loadingIndicator ?? throw new ArgumentNullException(nameof(loadingIndicator));
		_api = api ?? throw new ArgumentNullException(nameof(api));
	}

	public override string Title => Localizer.Instance.GetString("Tags");

	public ObservableCollection<Tag> Tags { get; } = new ObservableCollection<Tag>();

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
			var tags = await _api.GetTagsAsync();
			Tags.Clear();
			Tags.AddRange(tags.Content!.OrderBy(t => t.DisplayName));
		}
		catch (Exception ex)
		{
			await _dialogService.ShowStatusMessageAsync(
				StatusMessageDialogType.Error,
				"Could not load data",
				$"Error occurred loading data from server. {ex}");
		}
	}

	public ICommand AddTagCommand => GetOrCreateAsyncCommand(AddTagAsync);

	public ICommand UpdateTagCommand => GetOrCreateAsyncCommand<Tag>(UpdateTagAsync);

	private async Task AddTagAsync()
	{
		var viewModel = new AddOrUpdateTagDialogViewModel();
		var result = await _dialogService.ShowAsync(viewModel);
		if (result != ContentDialogResult.Primary)
		{
			return;
		}

		using var loadingScope = _loadingIndicator.BeginLoading();
		var apiResponse = await _api.AddTagAsync(new Tag()
		{
			DisplayName = viewModel.Tag.DisplayName,
			RouteName = viewModel.Tag.RouteName,
		});

		await RefreshListAsync();
	}

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

		using var loadingScope = _loadingIndicator.BeginLoading();
		if (result == ContentDialogResult.Primary)
		{
			var apiResponse = await _api.UpdateTagAsync(tag.Id, new EditTag()
			{
				DisplayName = viewModel.Tag.DisplayName,
				RouteName = viewModel.Tag.RouteName,
			});
		}
		else if (result == ContentDialogResult.Secondary)
		{
			var apiResponse = await _api.DeleteTagAsync(tag.Id);
		}

		await RefreshListAsync();
	}
}
