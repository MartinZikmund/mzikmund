using System.Collections.ObjectModel;
using MZikmund.Api.Client;
using MZikmund.DataContracts.Blog;
using MZikmund.Extensions;
using MZikmund.Models.Dialogs;
using MZikmund.Services.Account;
using MZikmund.Services.Dialogs;
using MZikmund.Services.Localization;
using MZikmund.Services.Navigation;

namespace MZikmund.ViewModels.Admin;

public partial class PostsManagerViewModel : PageViewModel
{
	private readonly IDialogService _dialogService;
	private readonly INavigationService _navigationService;
	private readonly IMZikmundApi _api;
	private readonly ILogger<PostsManagerViewModel> _logger;
	private readonly List<PostListItem> _allPosts = new();

	public PostsManagerViewModel(
		IMZikmundApi api,
		IDialogService dialogService,
		INavigationService navigationService,
		ILogger<PostsManagerViewModel> logger)
	{
		_dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
		_navigationService = navigationService;
		_api = api ?? throw new ArgumentNullException(nameof(api));
		_logger = logger;
	}

	public override string Title => Localizer.Instance.GetString("Posts");

	public ObservableCollection<PostListItem> VisiblePosts { get; } = new();

	[ObservableProperty]
	public partial string SearchText { get; set; } = "";

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CanLoadMore))]
	[NotifyCanExecuteChangedFor(nameof(LoadMoreCommand))]
	public partial int CurrentPage { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CanLoadMore))]
	[NotifyCanExecuteChangedFor(nameof(LoadMoreCommand))]
	public partial int TotalPages { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CountText))]
	public partial int TotalCount { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsEmpty))]
	public partial bool IsLoading { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CanLoadMore))]
	[NotifyCanExecuteChangedFor(nameof(LoadMoreCommand))]
	public partial bool IsLoadingMore { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsEmpty))]
	public partial bool HasError { get; set; }

	[ObservableProperty]
	public partial string? ErrorDetail { get; set; }

	public bool CanLoadMore => CurrentPage < TotalPages && !IsLoadingMore;

	public bool IsEmpty => !IsLoading && !HasError && VisiblePosts.Count == 0;

	public bool HasVisiblePosts => VisiblePosts.Count > 0;

	public string CountText => string.Format(System.Globalization.CultureInfo.CurrentCulture, Localizer.Instance.GetString("PostCountFormat"), TotalCount);

	partial void OnSearchTextChanged(string value) => ApplyFilter();

	public override async void ViewLoaded()
	{
		base.ViewLoaded();
		await RefreshCommand.ExecuteAsync(null);
	}

	[RelayCommand]
	private async Task RefreshAsync()
	{
		IsLoading = true;
		HasError = false;
		_allPosts.Clear();
		ApplyFilter();
		try
		{
			await LoadPageAsync(1);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Loading admin posts failed");
			ErrorDetail = ex is AuthenticationFailedException ? ex.Message : null;
			HasError = true;
		}
		finally
		{
			IsLoading = false;
		}
	}

	private async Task LoadPageAsync(int pageNumber)
	{
		var response = await _api.GetAllPostsAsync(pageNumber);
		await response.EnsureSuccessfulAsync();

		if (response.Content is { } page)
		{
			_allPosts.AddRange(page.Data);
			TotalCount = page.TotalCount;
			TotalPages = page.PageSize > 0 ? (int)Math.Ceiling((double)page.TotalCount / page.PageSize) : 1;
			CurrentPage = page.PageNumber;
			ApplyFilter();
		}
	}

	[RelayCommand(CanExecute = nameof(CanLoadMore))]
	private async Task LoadMoreAsync()
	{
		IsLoadingMore = true;
		try
		{
			await LoadPageAsync(CurrentPage + 1);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Loading more admin posts failed");
			await _dialogService.ShowStatusMessageAsync(
				StatusMessageDialogType.Error,
				Localizer.Instance.GetString("CouldNotLoadData"),
				Localizer.Instance.GetString("ErrorLoadingData"));
		}
		finally
		{
			IsLoadingMore = false;
		}
	}

	private void ApplyFilter()
	{
		var query = SearchText.Trim();
		var matches = string.IsNullOrEmpty(query) ?
			_allPosts :
			_allPosts.Where(p =>
				p.Title.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
				p.RouteName.Contains(query, StringComparison.OrdinalIgnoreCase));

		VisiblePosts.Clear();
		foreach (var post in matches)
		{
			VisiblePosts.Add(post);
		}

		OnPropertyChanged(nameof(IsEmpty));
		OnPropertyChanged(nameof(HasVisiblePosts));
	}

	[RelayCommand]
	private void AddPost() => _navigationService.Navigate<PostEditorViewModel>(Guid.Empty);

	public void EditPost(PostListItem post) => _navigationService.Navigate<PostEditorViewModel>(post.Id);
}
