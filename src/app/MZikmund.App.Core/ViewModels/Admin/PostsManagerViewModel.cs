using System.Collections.ObjectModel;
using MZikmund.Api.Client;
using MZikmund.DataContracts.Blog;
using MZikmund.Extensions;
using MZikmund.Models.Dialogs;
using MZikmund.Services.Dialogs;
using MZikmund.Services.Loading;
using MZikmund.Services.Localization;
using MZikmund.Services.Navigation;
using Newtonsoft.Json;
using Windows.Storage.Pickers;

namespace MZikmund.ViewModels.Admin;

public class PostsManagerViewModel : PageViewModel
{
	private readonly IDialogService _dialogService;
	private readonly ILoadingIndicator _loadingIndicator;
	private readonly INavigationService _navigationService;
	private readonly IMZikmundApi _api;

	public PostsManagerViewModel(
		IMZikmundApi api,
		IDialogService dialogService,
		ILoadingIndicator loadingIndicator,
		INavigationService navigationService)
	{
		_dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
		_loadingIndicator = loadingIndicator ?? throw new ArgumentNullException(nameof(loadingIndicator));
		_navigationService = navigationService;
		_api = api ?? throw new ArgumentNullException(nameof(api));
	}

	public override string Title => Localizer.Instance.GetString("Posts");

	public ObservableCollection<PostListItem> Posts { get; } = new ObservableCollection<PostListItem>();

	public override async void ViewLoaded()
	{
		await RefreshListAsync();
	}

	private async Task RefreshListAsync()
	{
		using var loadingScope = _loadingIndicator.BeginLoading();
		try
		{
			//TODO: Refresh collection based on IDs
			var posts = await _api.GetPostsAsync();
			Posts.Clear();
			Posts.AddRange(posts.Content!.Data.OrderByDescending(t => t.LastModifiedDate));
		}
		catch (Exception ex)
		{
			await _dialogService.ShowStatusMessageAsync(
				StatusMessageDialogType.Error,
				"Could not load data",
				$"Error occurred loading data from server. {ex}");
		}
	}

	public ICommand AddPostCommand => GetOrCreateCommand(AddPost);

	public ICommand UpdatePostCommand => GetOrCreateCommand<PostListItem>(UpdatePost);

	private void AddPost()
	{
		_navigationService.Navigate<PostEditorViewModel>(Guid.Empty);
	}

	private void UpdatePost(PostListItem? post)
	{
		if (post is null)
		{
			return;
		}

		_navigationService.Navigate<PostEditorViewModel>(post.Id);
	}
}
