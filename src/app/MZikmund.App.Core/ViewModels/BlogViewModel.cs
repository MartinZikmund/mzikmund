using System.Collections.ObjectModel;
using MZikmund.Api.Client;
using MZikmund.DataContracts.Blog;
using MZikmund.Services.Loading;
using MZikmund.Services.Localization;
using MZikmund.Services.Navigation;
using MZikmund.ViewModels.Items;
using MZikmund.Web.Core.Services;

namespace MZikmund.ViewModels;

public partial class BlogViewModel : PageViewModel
{
	private readonly IMZikmundApi _api;
	private readonly ILoadingIndicator _loadingIndicator;
	private readonly IMarkdownConverter _markdownConverter;
	private readonly INavigationService _navigationService;

	[ObservableProperty]
	private int _currentPage = 1;

	[ObservableProperty]
	private int _totalPages = 1;

	[ObservableProperty]
	private int _totalCount = 0;

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
	}

	public override string Title => Localizer.Instance.GetString("Blog");

	public ObservableCollection<PostListItemViewModel> Posts { get; } = new();

	public bool CanGoToPreviousPage => CurrentPage > 1;

	public bool CanGoToNextPage => CurrentPage < TotalPages;

	public override async void ViewNavigatedTo(object? parameter)
	{
		base.ViewNavigatedTo(parameter);
		await LoadPostsAsync();
	}

	private async Task LoadPostsAsync()
	{
		using var _ = _loadingIndicator.BeginLoading();
		Posts.Clear();
		var response = await _api.GetPostsAsync(CurrentPage);
		var pagedResponse = response.Content!;
		
		TotalCount = pagedResponse.TotalCount;
		TotalPages = (int)Math.Ceiling((double)pagedResponse.TotalCount / pagedResponse.PageSize);

		foreach (var post in pagedResponse.Data)
		{
			Posts.Add(new(post, _markdownConverter));
		}

		OnPropertyChanged(nameof(CanGoToPreviousPage));
		OnPropertyChanged(nameof(CanGoToNextPage));
	}

	[RelayCommand(CanExecute = nameof(CanGoToPreviousPage))]
	private async Task GoToPreviousPageAsync()
	{
		CurrentPage--;
		await LoadPostsAsync();
		GoToPreviousPageCommand.NotifyCanExecuteChanged();
		GoToNextPageCommand.NotifyCanExecuteChanged();
	}

	[RelayCommand(CanExecute = nameof(CanGoToNextPage))]
	private async Task GoToNextPageAsync()
	{
		CurrentPage++;
		await LoadPostsAsync();
		GoToPreviousPageCommand.NotifyCanExecuteChanged();
		GoToNextPageCommand.NotifyCanExecuteChanged();
	}

	public void ItemClicked(object sender, ItemClickEventArgs args)
	{
		var item = (PostListItemViewModel)args.ClickedItem;
		_navigationService.Navigate<PostViewModel>(item.Item.Id);
	}
}
