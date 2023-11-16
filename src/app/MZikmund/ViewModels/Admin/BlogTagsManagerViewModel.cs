using System.Collections.ObjectModel;
using MZikmund.Api.Client;
using MZikmund.Extensions;
using MZikmund.Models.Dialogs;
using MZikmund.Services.Dialogs;
using MZikmund.Services.Loading;
using Newtonsoft.Json;
using Windows.Storage.Pickers;
using MZikmund.Services.Localization;
using MZikmund.DataContracts.Blog;

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
		using var loadingScope = _loadingIndicator.BeginLoading();
		try
		{
			//TODO: Refresh collection based on IDs
			var tags = await _api.GetTagsAsync();
			Tags.AddRange(tags.Content!);
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

	public ICommand ImportJsonCommand => GetOrCreateAsyncCommand(ImportJsonAsync);

	private async Task ImportJsonAsync()
	{
		using var loadingScope = _loadingIndicator.BeginLoading();
		var picker = new FileOpenPicker();
		picker.FileTypeFilter.Add(".json");
		var jsonFile = await picker.PickSingleFileAsync();
		var jsonContent = await FileIO.ReadTextAsync(jsonFile);
		var tags = JsonConvert.DeserializeObject<Tag[]>(jsonContent);
		if (tags == null)
		{
			return;
		}

		for (int i = 0; i < tags.Length; i++)
		{
			var tag = tags[i];
			_loadingIndicator.StatusMessage = $"Adding tag {i + 1} of {tags.Length}";
			// Ensure tag ID is empty.
			tag.Id = Guid.Empty;

			await _api.AddTagAsync(tag);
		}
	}

	private async Task AddTagAsync()
	{
		var viewModel = new AddOrUpdateTagDialogViewModel();
		var result = await _dialogService.ShowAsync(viewModel);
		if (result != ContentDialogResult.Primary)
		{
			return;
		}

		//var apiResponse = await _api.AddTagAsync(new TagDto()
		//{
		//	Localizations = new[]
		//	{
		//		new TagLocalizationDto()
		//		{
		//			DisplayName = "test",
		//			LanguageId = 1,
		//			RouteName = "test"
		//		}
		//	}
		//});
	}

	public async Task UpdateTagAsync(Tag dto)
	{
		var viewModel = new AddOrUpdateTagDialogViewModel();
		var result = await _dialogService.ShowAsync(viewModel);
		if (result != ContentDialogResult.Primary)
		{
			return;
		}

		await Task.CompletedTask;

		//var apiResponse = await _api.AddTagAsync(new TagDto()
		//{
		//	Localizations = new[]
		//	{
		//		new TagLocalizationDto()
		//		{
		//			DisplayName = "test",
		//			LanguageId = 1,
		//			RouteName = "test"
		//		}
		//	}
		//});
	}
}
