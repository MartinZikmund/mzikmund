using System.Windows.Input;
using MZikmund.Api.Client;
using MZikmund.Collections;
using MZikmund.Services.Localization;
using MZikmund.Services.Navigation;
using MZikmund.ViewModels.Items;
using MZikmund.Web.Core.Services;

namespace MZikmund.ViewModels;

public class BlogViewModel : PageViewModel
{
	private readonly IMZikmundApi _api;
	private readonly IMarkdownConverter _markdownConverter;
	private readonly INavigationService _navigationService;
	private readonly ILogger<BlogViewModel> _logger;
	private int _reloadGeneration;
	private bool _firstPageFailed;
	private bool _isLoading;
	private bool _isLoadingMore;
	private bool _hasError;
	private bool _hasLoadMoreError;

	public BlogViewModel(
		IMZikmundApi api,
		IMarkdownConverter markdownConverter,
		INavigationService navigationService,
		ILogger<BlogViewModel> logger)
	{
		_api = api ?? throw new ArgumentNullException(nameof(api));
		_markdownConverter = markdownConverter;
		_navigationService = navigationService;
		_logger = logger;
		Posts = new(LoadPageAsync);
		Posts.CollectionChanged += (_, _) => OnPropertyChanged(nameof(IsEmpty));
		RetryCommand = new AsyncRelayCommand(ReloadAsync);
		RetryLoadMoreCommand = new AsyncRelayCommand(RetryLoadMoreAsync);
	}

	public override string Title => Localizer.Instance.GetString("Blog");

	public string Lead => Localizer.Instance.GetString("BlogLead");

	public IncrementalLoadingCollection<PostListItemViewModel> Posts { get; }

	/// <summary>
	/// Gets the command that throws the list away and reloads it from the first page.
	/// </summary>
	public ICommand RetryCommand { get; }

	/// <summary>
	/// Gets the command that re-requests the page whose load failed, keeping the pages already shown.
	/// </summary>
	public ICommand RetryLoadMoreCommand { get; }

	/// <summary>
	/// Gets a value indicating whether the first page is in flight.
	/// </summary>
	public bool IsLoading
	{
		get => _isLoading;
		private set
		{
			SetProperty(ref _isLoading, value);
			OnPropertyChanged(nameof(IsEmpty));
		}
	}

	/// <summary>
	/// Gets a value indicating whether a follow-up page is in flight.
	/// </summary>
	public bool IsLoadingMore
	{
		get => _isLoadingMore;
		private set => SetProperty(ref _isLoadingMore, value);
	}

	/// <summary>
	/// Gets a value indicating whether the first page failed. It only ever turns true in the same
	/// synchronous step that clears <see cref="IsLoading"/>, and a first page only ever loads into
	/// an emptied list, so the error, loading and empty overlays are mutually exclusive by
	/// construction.
	/// </summary>
	public bool HasError
	{
		get => _hasError;
		private set
		{
			SetProperty(ref _hasError, value);
			OnPropertyChanged(nameof(IsEmpty));
		}
	}

	/// <summary>
	/// Gets a value indicating whether a follow-up page failed. Surfaced inline under the cards
	/// rather than as a full page error, because the pages already loaded are still valid.
	/// </summary>
	public bool HasLoadMoreError
	{
		get => _hasLoadMoreError;
		private set => SetProperty(ref _hasLoadMoreError, value);
	}

	public bool IsEmpty => !IsLoading && !HasError && Posts.Count == 0;

	public override async void ViewNavigatedTo(object? parameter)
	{
		base.ViewNavigatedTo(parameter);
		await ReloadAsync();
	}

	public void NavigateToPost(Guid postId) => _navigationService.Navigate<PostViewModel>(postId);

	private async Task ReloadAsync()
	{
		var generation = ++_reloadGeneration;
		_firstPageFailed = false;
		HasError = false;
		HasLoadMoreError = false;
		IsLoading = true;
		Posts.Reset();
		try
		{
			await Posts.LoadMoreItemsAsync(0);
		}
		finally
		{
			if (generation == _reloadGeneration)
			{
				HasError = _firstPageFailed;
				IsLoading = false;
			}
		}
	}

	private async Task RetryLoadMoreAsync()
	{
		HasLoadMoreError = false;
		await Posts.LoadMoreItemsAsync(0);
	}

	private async Task<IncrementalPage<PostListItemViewModel>> LoadPageAsync(int pageNumber, CancellationToken cancellationToken)
	{
		IsLoadingMore = pageNumber > 1;
		try
		{
			var response = await _api.GetPostsAsync(pageNumber, cancellationToken);
			if (cancellationToken.IsCancellationRequested)
			{
				return IncrementalPage<PostListItemViewModel>.Failure;
			}

			if (response.Content is not { } content)
			{
				_logger.LogError(response.Error, "Loading posts page {PageNumber} returned {StatusCode}", pageNumber, response.StatusCode);
				return Fail(pageNumber);
			}

			var items = content.Data.Select(post => new PostListItemViewModel(post, _markdownConverter)).ToArray();
			var totalPages = content.PageSize > 0
				? (int)Math.Ceiling((double)content.TotalCount / content.PageSize)
				: 1;

			return new IncrementalPage<PostListItemViewModel>(items, content.PageNumber < totalPages);
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			// The request itself is now abortable, so a discarded page arrives as a cancellation
			// rather than as data. It is not an error state.
			return IncrementalPage<PostListItemViewModel>.Failure;
		}
		catch (Exception ex)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return IncrementalPage<PostListItemViewModel>.Failure;
			}

			_logger.LogError(ex, "Loading posts page {PageNumber} failed", pageNumber);
			return Fail(pageNumber);
		}
		finally
		{
			if (!cancellationToken.IsCancellationRequested)
			{
				IsLoadingMore = false;
			}
		}
	}

	private IncrementalPage<PostListItemViewModel> Fail(int pageNumber)
	{
		if (pageNumber == 1)
		{
			// Read back by ReloadAsync so HasError flips in the same step that clears IsLoading.
			_firstPageFailed = true;
		}
		else
		{
			HasLoadMoreError = true;
		}

		return IncrementalPage<PostListItemViewModel>.Failure;
	}
}
