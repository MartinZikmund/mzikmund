using System.Collections.ObjectModel;
using Microsoft.UI.Dispatching;
using MZikmund.Api.Client;
using MZikmund.DataContracts.Blog;
using MZikmund.Extensions;
using MZikmund.Models.Dialogs;
using MZikmund.Services.Account;
using MZikmund.Services.Dialogs;
using MZikmund.Services.Localization;
using MZikmund.Services.Navigation;
using MZikmund.Services.Timers;
using MZikmund.ViewModels.Items;

namespace MZikmund.ViewModels.Admin;

public partial class PostsManagerViewModel : PageViewModel
{
	private readonly IDialogService _dialogService;
	private readonly INavigationService _navigationService;
	private readonly IMZikmundApi _api;
	private readonly ITimerFactory _timerFactory;
	private readonly ILogger<PostsManagerViewModel> _logger;
	private readonly List<AdminPostItemViewModel> _allPosts = new();
	private DispatcherQueueTimer? _scheduleTimer;
	private int _loadGeneration;

	public PostsManagerViewModel(
		IMZikmundApi api,
		IDialogService dialogService,
		INavigationService navigationService,
		ITimerFactory timerFactory,
		ILogger<PostsManagerViewModel> logger)
	{
		_dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
		_navigationService = navigationService;
		_api = api ?? throw new ArgumentNullException(nameof(api));
		_timerFactory = timerFactory;
		_logger = logger;
	}

	public override string Title => Localizer.Instance.GetString("Posts");

	public ObservableCollection<AdminPostItemViewModel> VisiblePosts { get; } = new();

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
	[NotifyPropertyChangedFor(nameof(IsEmpty))]
	[NotifyCanExecuteChangedFor(nameof(LoadMoreCommand))]
	public partial bool IsLoadingMore { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsEmpty))]
	public partial bool HasError { get; set; }

	[ObservableProperty]
	public partial string? ErrorDetail { get; set; }

	public bool CanLoadMore => CurrentPage < TotalPages && !IsLoadingMore;

	public bool IsEmpty => !IsLoading && !IsLoadingMore && !HasError && VisiblePosts.Count == 0;

	public bool HasVisiblePosts => VisiblePosts.Count > 0;

	public string CountText => string.Format(System.Globalization.CultureInfo.CurrentCulture, Localizer.Instance.GetString("PostCountFormat"), TotalCount);

	private bool IsSearching => !string.IsNullOrWhiteSpace(SearchText);

	partial void OnSearchTextChanged(string value)
	{
		ApplyFilter();
		LoadRemainingPagesForSearch();
	}

	// The API has no search, so a query can only match what has been fetched.
	private void LoadRemainingPagesForSearch()
	{
		if (IsSearching && CanLoadMore)
		{
			_ = LoadMoreCommand.ExecuteAsync(null);
		}
	}

	public override async void ViewLoaded()
	{
		base.ViewLoaded();

		if (_scheduleTimer is null)
		{
			_scheduleTimer = _timerFactory.CreateTimer();
			_scheduleTimer.Interval = TimeSpan.FromSeconds(1);
			_scheduleTimer.Tick += (_, _) => UpdateSchedules(DateTimeOffset.UtcNow);
		}

		_scheduleTimer.Start();
		await RefreshCommand.ExecuteAsync(null);
	}

	public override void ViewUnloaded()
	{
		base.ViewUnloaded();
		_scheduleTimer?.Stop();
	}

	// A row only ever leaves Scheduled, so the settled ones need no work.
	public void UpdateSchedules(DateTimeOffset now)
	{
		foreach (var post in _allPosts)
		{
			if (post.IsScheduled)
			{
				post.UpdateSchedule(now);
			}
		}
	}

	[RelayCommand]
	private async Task RefreshAsync()
	{
		// Each refresh owns the list from here on; anything still in flight must not touch it.
		var generation = ++_loadGeneration;
		IsLoading = true;
		IsLoadingMore = false;
		HasError = false;
		CurrentPage = 0;
		TotalPages = 0;
		_allPosts.Clear();
		ApplyFilter();
		try
		{
			await LoadPageAsync(1, generation);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Loading admin posts failed");
			if (generation == _loadGeneration)
			{
				ErrorDetail = ex is AuthenticationFailedException ? ex.Message : null;
				HasError = true;
			}
		}
		finally
		{
			if (generation == _loadGeneration)
			{
				IsLoading = false;
			}
		}

		if (generation == _loadGeneration)
		{
			LoadRemainingPagesForSearch();
		}
	}

	private async Task LoadPageAsync(int pageNumber, int generation)
	{
		var response = await _api.GetAllPostsAsync(pageNumber);
		await response.EnsureSuccessfulAsync();

		if (generation == _loadGeneration && response.Content is { } page)
		{
			var now = DateTimeOffset.UtcNow;
			_allPosts.AddRange(page.Data.Select(post => new AdminPostItemViewModel(post, now)));
			TotalCount = page.TotalCount;
			TotalPages = page.PageSize > 0 ? (int)Math.Ceiling((double)page.TotalCount / page.PageSize) : 1;
			CurrentPage = page.PageNumber;
			ApplyFilter();
		}
	}

	[RelayCommand(CanExecute = nameof(CanLoadMore))]
	private async Task LoadMoreAsync()
	{
		var generation = _loadGeneration;
		var pageNumber = CurrentPage;
		IsLoadingMore = true;
		try
		{
			do
			{
				await LoadPageAsync(++pageNumber, generation);
			}
			while (generation == _loadGeneration && IsSearching && pageNumber < TotalPages);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Loading more admin posts failed");
			if (generation == _loadGeneration)
			{
				await _dialogService.ShowStatusMessageAsync(
					StatusMessageDialogType.Error,
					Localizer.Instance.GetString("CouldNotLoadData"),
					Localizer.Instance.GetString("ErrorLoadingData"));
			}
		}
		finally
		{
			if (generation == _loadGeneration)
			{
				IsLoadingMore = false;
			}
		}
	}

	private void ApplyFilter()
	{
		var query = SearchText.Trim();
		var matches = string.IsNullOrEmpty(query) ?
			_allPosts :
			_allPosts.Where(p =>
				p.Post.Title.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
				p.Post.RouteName.Contains(query, StringComparison.OrdinalIgnoreCase));

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
