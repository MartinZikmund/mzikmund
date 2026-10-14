using System.Collections.ObjectModel;
using System.Globalization;
using MZikmund.Api.Client;
using MZikmund.DataContracts;
using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Models.Dialogs;
using MZikmund.Services.Comments;
using MZikmund.Services.Dialogs;
using MZikmund.Services.Localization;
using Refit;

namespace MZikmund.ViewModels.Admin;

public partial class CommentsModeratorViewModel : PageViewModel
{
	private readonly IMZikmundApi _api;
	private readonly IDialogService _dialogService;
	private readonly ICommentsBadgeService _badge;
	private readonly ILogger<CommentsModeratorViewModel> _logger;
	private int _feedPage;
	private int _loadVersion;

	public CommentsModeratorViewModel(
		IMZikmundApi api,
		IDialogService dialogService,
		ICommentsBadgeService badge,
		ILogger<CommentsModeratorViewModel> logger)
	{
		_api = api;
		_dialogService = dialogService;
		_badge = badge;
		_logger = logger;
	}

	public override string Title => Localizer.Instance.GetString("Comments");

	public ObservableCollection<CommentReportItemViewModel> Reports { get; } = new();

	public ObservableCollection<CommentReportItemViewModel> Feed { get; } = new();

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsQueueEmpty))]
	public partial bool IsLoading { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsQueueEmpty))]
	public partial bool HasError { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CanLoadMore))]
	public partial bool IsLoadingMore { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CanLoadMore))]
	public partial bool HasMorePages { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CountText))]
	public partial int ReportCount { get; set; }

	public bool CanLoadMore => HasMorePages && !IsLoadingMore;

	public bool IsQueueEmpty => !IsLoading && !HasError && Reports.Count == 0;

	public bool HasReports => Reports.Count > 0;

	public bool HasFeed => Feed.Count > 0;

	public string CountText => string.Format(CultureInfo.CurrentCulture, Localizer.Instance.GetString("ModeratorAwaitingFormat"), ReportCount);

	public override async void ViewLoaded()
	{
		base.ViewLoaded();
		await RefreshAsync();
	}

	[RelayCommand]
	private async Task RefreshAsync()
	{
		var version = ++_loadVersion;
		IsLoading = true;
		HasError = false;
		try
		{
			// The queue is the whole point of the page, so it is read in full; the feed is paged.
			var reports = await LoadAllReportsAsync();
			var feed = await _api.GetModerationFeedAsync(null, 1);
			await feed.EnsureSuccessfulAsync();
			if (version != _loadVersion)
			{
				return;
			}

			Reports.Clear();
			foreach (var report in reports)
			{
				Reports.Add(new CommentReportItemViewModel(report));
			}

			ReportCount = Reports.Count;
			Feed.Clear();
			_feedPage = 0;
			AppendFeed(feed.Content!);

			// Only once the feed has actually been shown does it count as seen.
			_badge.MarkSeen();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Loading the moderation page failed");
			if (version == _loadVersion)
			{
				HasError = true;
			}
		}
		finally
		{
			if (version == _loadVersion)
			{
				IsLoading = false;
				NotifyListsChanged();
			}
		}
	}

	private async Task<List<CommentReportDto>> LoadAllReportsAsync()
	{
		List<CommentReportDto> all = new();
		var pageNumber = 1;
		while (true)
		{
			var response = await _api.GetCommentReportsAsync(pageNumber);
			await response.EnsureSuccessfulAsync();
			var page = response.Content!;
			all.AddRange(page.Data);
			if (!page.Data.Any() || pageNumber * page.PageSize >= page.TotalCount)
			{
				return all;
			}

			pageNumber++;
		}
	}

	private void AppendFeed(PagedResponse<CommentModerationDto> page)
	{
		foreach (var comment in page.Data)
		{
			Feed.Add(new CommentReportItemViewModel(comment));
		}

		_feedPage = page.PageNumber;
		HasMorePages = page.PageNumber * page.PageSize < page.TotalCount;
	}

	[RelayCommand]
	private async Task LoadMoreAsync()
	{
		if (!CanLoadMore)
		{
			return;
		}

		IsLoadingMore = true;
		try
		{
			var response = await _api.GetModerationFeedAsync(null, _feedPage + 1);
			await response.EnsureSuccessfulAsync();
			AppendFeed(response.Content!);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Loading more moderation feed entries failed");
			await ShowFailureAsync();
		}
		finally
		{
			IsLoadingMore = false;
			NotifyListsChanged();
		}
	}

	public Task HideAsync(CommentReportItemViewModel item) =>
		RunAsync(() => _api.HideCommentAsync(item.Id));

	public async Task DeleteAsync(CommentReportItemViewModel item)
	{
		if (await ConfirmAsync("ModeratorDeleteTitle", "ModeratorDeleteText"))
		{
			await RunAsync(() => _api.AdminDeleteCommentAsync(item.Id));
		}
	}

	public async Task BanAsync(CommentReportItemViewModel item)
	{
		if (await ConfirmAsync("ModeratorBanTitle", "ModeratorBanText"))
		{
			await RunAsync(() => _api.BanCommentAuthorAsync(item.AuthorId));
		}
	}

	public Task DismissAsync(CommentReportItemViewModel item) =>
		RunAsync(() => _api.ResolveCommentReportAsync(item.Id));

	private async Task<bool> ConfirmAsync(string titleKey, string textKey) =>
		await _dialogService.ShowConfirmationDialogAsync(
			Localizer.Instance.GetString(titleKey),
			Localizer.Instance.GetString(textKey)) == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary;

	// Every action can change both lists (a hide closes its reports, a ban hides a whole author), so the page reloads.
	private async Task RunAsync(Func<Task<IApiResponse>> action)
	{
		try
		{
			var response = await action();
			if (response.Error is { } error)
			{
				throw error;
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "A moderation action failed");
			await ShowFailureAsync();
			return;
		}

		await RefreshAsync();
	}

	private async Task ShowFailureAsync() =>
		_dialogService.ShowStatusMessageAsync(
			StatusMessageDialogType.Error,
			Localizer.Instance.GetString("CommentActionFailed"),
			Localizer.Instance.GetString("ErrorLoadingData"));

	private void NotifyListsChanged()
	{
		OnPropertyChanged(nameof(IsQueueEmpty));
		OnPropertyChanged(nameof(HasReports));
		OnPropertyChanged(nameof(HasFeed));
	}
}
