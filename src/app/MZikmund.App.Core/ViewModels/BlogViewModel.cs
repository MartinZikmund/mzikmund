using System.Collections.ObjectModel;
using System.Windows.Input;
using MZikmund.Api.Client;
using MZikmund.DataContracts.Blog;
using MZikmund.Services.Loading;
using MZikmund.Services.Localization;
using MZikmund.Services.Navigation;
using MZikmund.ViewModels.Items;
using MZikmund.Web.Core.Services;

namespace MZikmund.ViewModels;

public class BlogViewModel : PageViewModel
{
	private readonly IMZikmundApi _api;
	private readonly ILoadingIndicator _loadingIndicator;
	private readonly IMarkdownConverter _markdownConverter;
	private readonly INavigationService _navigationService;
	private int _currentPage = 1;
	private int _totalPages = 1;
	private int _totalCount;
	private bool _isLoadingMore;

	public BlogViewModel(
		IMZikmundApi api,
		ILoadingIndicator loadingIndicator,
		IMarkdownConverter markdownConverter,
		INavigationService navigationService)
	{
		_api = api ?? throw new ArgumentNullException(nameof(api));
		_loadingIndicator = loadingIndicator;
		_markdownConverter = markdownConverter;
		_navigationService = navigationService;
		LoadMoreCommand = new AsyncRelayCommand(LoadMoreAsync, () => CanLoadMore);
	}

	public override string Title => Localizer.Instance.GetString("Blog");

	public ObservableCollection<PostListItemViewModel> Posts { get; } = new();

	public int CurrentPage
	{
		get => _currentPage;
		private set
		{
			if (SetProperty(ref _currentPage, value))
			{
				OnPropertyChanged(nameof(CanLoadMore));
			}
		}
	}

	public int TotalPages
	{
		get => _totalPages;
		private set
		{
			if (SetProperty(ref _totalPages, value))
			{
				OnPropertyChanged(nameof(CanLoadMore));
			}
		}
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
			if (SetProperty(ref _isLoadingMore, value))
			{
				OnPropertyChanged(nameof(CanLoadMore));
				(LoadMoreCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
			}
		}
	}

	public bool CanLoadMore => CurrentPage < TotalPages && !IsLoadingMore;

	public ICommand LoadMoreCommand { get; }

	public override async void ViewNavigatedTo(object? parameter)
	{
		base.ViewNavigatedTo(parameter);
		using var _ = _loadingIndicator.BeginLoading();
		Posts.Clear();
		CurrentPage = 1;
		await LoadPostsAsync(CurrentPage);
	}

	private async Task LoadPostsAsync(int pageNumber)
	{
		var response = await _api.GetPostsAsync(pageNumber);
		if (response.Content != null)
		{
			foreach (var post in response.Content.Data)
			{
				Posts.Add(new(post, _markdownConverter));
			}

			TotalCount = response.Content.TotalCount;
			TotalPages = (int)Math.Ceiling((double)response.Content.TotalCount / response.Content.PageSize);
			CurrentPage = response.Content.PageNumber;
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
		finally
		{
			IsLoadingMore = false;
		}
	}

	public void ItemClicked(object sender, ItemClickEventArgs args)
	{
		var item = (PostListItemViewModel)args.ClickedItem;
		_navigationService.Navigate<PostViewModel>(item.Item.Id);
	}

	public void NavigateToPost(Guid postId)
	{
		_navigationService.Navigate<PostViewModel>(postId);
	}
}
