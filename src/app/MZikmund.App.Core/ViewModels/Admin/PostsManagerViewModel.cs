using System.Collections.ObjectModel;
using System.Windows.Input;
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

public partial class PostsManagerViewModel : PageViewModel
{
	private readonly IDialogService _dialogService;
	private readonly ILoadingIndicator _loadingIndicator;
	private readonly INavigationService _navigationService;
	private readonly IMZikmundApi _api;
	private int _currentPage = 1;
	private int _totalPages = 1;
	private int _totalCount;
	private bool _isLoadingMore;

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
		LoadMoreCommand = new AsyncRelayCommand(LoadMoreAsync, () => CanLoadMore);
	}

	public override string Title => Localizer.Instance.GetString("Posts");

	public ObservableCollection<PostListItem> Posts { get; } = new ObservableCollection<PostListItem>();

	public int CurrentPage
	{
		get => _currentPage;
		private set => SetProperty(ref _currentPage, value);
	}

	public int TotalPages
	{
		get => _totalPages;
		private set => SetProperty(ref _totalPages, value);
	}

	public int TotalCount
	{
		get => _totalCount;
		private set => SetProperty(ref _totalCount, value);
	}

	public bool IsLoadingMore
	{
		get => _isLoadingMore;
		private set
		{
			SetProperty(ref _isLoadingMore, value);
			(LoadMoreCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
			OnPropertyChanged(nameof(CanLoadMore));
		}
	}

	public bool CanLoadMore => CurrentPage < TotalPages && !IsLoadingMore;

	public ICommand LoadMoreCommand { get; }

	public override async void ViewLoaded()
	{
		await RefreshListAsync();
	}

	private async Task RefreshListAsync()
	{
		using var loadingScope = _loadingIndicator.BeginLoading();
		try
		{
			Posts.Clear();
			CurrentPage = 1;
			await LoadPostsAsync(1);
		}
		catch (Exception ex)
		{
			await _dialogService.ShowStatusMessageAsync(
				StatusMessageDialogType.Error,
				"Could not load data",
				$"Error occurred loading data from server. {ex}");
		}
	}

	private async Task LoadPostsAsync(int pageNumber)
	{
		var posts = await _api.GetAllPostsAsync(pageNumber);
		await posts.EnsureSuccessfulAsync();

		if (posts.Content != null)
		{
			Posts.AddRange(posts.Content.Data);
			TotalCount = posts.Content.TotalCount;
			TotalPages = (int)Math.Ceiling((double)posts.Content.TotalCount / posts.Content.PageSize);
			CurrentPage = posts.Content.PageNumber;
		}
	}

	private async Task LoadMoreAsync()
	{
		if (!CanLoadMore)
		{
			return;
		}

		IsLoadingMore = true;
		try
		{
			await LoadPostsAsync(CurrentPage + 1);
		}
		catch (Exception ex)
		{
			await _dialogService.ShowStatusMessageAsync(
				StatusMessageDialogType.Error,
				"Could not load data",
				$"Error occurred loading data from server. {ex}");
		}
		finally
		{
			IsLoadingMore = false;
		}
	}

	[RelayCommand]
	private void AddPost()
	{
		_navigationService.Navigate<PostEditorViewModel>(Guid.Empty);
	}

	[RelayCommand]
	private void UpdatePost(PostListItem? post)
	{
		if (post is null)
		{
			return;
		}

		_navigationService.Navigate<PostEditorViewModel>(post.Id);
	}
}
