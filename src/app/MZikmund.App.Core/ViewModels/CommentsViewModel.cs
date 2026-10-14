using System.Collections.ObjectModel;
using System.Globalization;
using System.Net;
using System.Text.Json;
using Humanizer;
using MZikmund.Api.Client;
using MZikmund.DataContracts;
using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Models.Dialogs;
using MZikmund.Services.Account;
using MZikmund.Services.Dialogs;
using MZikmund.Services.Localization;
using MZikmund.Services.Navigation;
using MZikmund.ViewModels.Items;
using Refit;

namespace MZikmund.ViewModels;

public partial class CommentsViewModel : ViewModelBase
{
	private readonly IMZikmundApi _api;
	private readonly IUserService _userService;
	private readonly ISignInPrompt _signInPrompt;
	private readonly IDialogService _dialogService;
	private readonly IWindowShellProvider _shellProvider;
	private readonly ILogger<CommentsViewModel> _logger;
	private Guid _postId;
	private int _currentPage;
	private int _loadVersion;
	private Task _loadTask = Task.CompletedTask;

	public CommentsViewModel(
		IMZikmundApi api,
		IUserService userService,
		ISignInPrompt signInPrompt,
		IDialogService dialogService,
		IWindowShellProvider shellProvider,
		ILogger<CommentsViewModel> logger)
	{
		_api = api;
		_userService = userService;
		_signInPrompt = signInPrompt;
		_dialogService = dialogService;
		_shellProvider = shellProvider;
		_logger = logger;
	}

	public ObservableCollection<CommentItemViewModel> Comments { get; } = new();

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsEmpty))]
	[NotifyPropertyChangedFor(nameof(CanLoadMore))]
	public partial bool IsLoading { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsEmpty))]
	public partial bool HasError { get; set; }

	// The feature gate answers 404 while comments are switched off; the panel then disappears.
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsPanelVisible))]
	public partial bool IsDisabled { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CountText))]
	public partial int TotalCount { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsSortTop))]
	[NotifyPropertyChangedFor(nameof(IsSortNew))]
	public partial CommentSortOrder Sort { get; set; } = CommentSortOrder.Top;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CanLoadMore))]
	public partial bool IsLoadingMore { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CanLoadMore))]
	public partial bool HasMorePages { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(DraftLength))]
	[NotifyPropertyChangedFor(nameof(IsDraftTooLong))]
	[NotifyPropertyChangedFor(nameof(DraftLengthText))]
	[NotifyCanExecuteChangedFor(nameof(PostCommand))]
	public partial string DraftText { get; set; } = "";

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsReplying))]
	[NotifyPropertyChangedFor(nameof(ReplyingToText))]
	public partial CommentItemViewModel? ReplyingTo { get; set; }

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(PostCommand))]
	public partial bool IsPosting { get; set; }

	public bool IsSortTop => Sort == CommentSortOrder.Top;

	public bool IsSortNew => Sort == CommentSortOrder.New;

	public bool IsEmpty => !IsLoading && !HasError && Comments.Count == 0;

	public bool IsPanelVisible => !IsDisabled;

	public bool CanLoadMore => HasMorePages && !IsLoadingMore && !IsLoading;

	public int DraftLength => DraftText.Length;

	public bool IsDraftTooLong => DraftLength > CommentLimits.MaxCommentLength;

	public string DraftLengthText => $"{DraftLength} / {CommentLimits.MaxCommentLength}";

	public bool IsReplying => ReplyingTo is not null;

	public string ReplyingToText => ReplyingTo is { } target ?
		string.Format(CultureInfo.CurrentCulture, Localizer.Instance.GetString("CommentReplyingToFormat"), target.DisplayName) :
		"";

	public string CountText => TotalCount.ToString(CultureInfo.CurrentCulture);

	partial void OnSortChanged(CommentSortOrder value)
	{
		if (_postId != Guid.Empty)
		{
			_ = ReloadAsync();
		}
	}

	/// <summary>
	/// Subscribes to sign-in changes so the has-upvoted and is-own flags stay right. Paired with <see cref="Detach"/>.
	/// </summary>
	public void Attach() => _userService.AuthenticationChanged += OnAuthenticationChanged;

	public void Detach() => _userService.AuthenticationChanged -= OnAuthenticationChanged;

	public Task LoadAsync(Guid postId)
	{
		_postId = postId;
		DraftText = "";
		ReplyingTo = null;
		return ReloadAsync();
	}

	[RelayCommand]
	private Task RetryAsync() => ReloadAsync();

	private void OnAuthenticationChanged(object? sender, EventArgs e)
	{
		if (_postId == Guid.Empty)
		{
			return;
		}

		if (_shellProvider.DispatcherQueue.HasThreadAccess)
		{
			_ = ReloadAsync();
		}
		else
		{
			_shellProvider.DispatcherQueue.TryEnqueue(() => _ = ReloadAsync());
		}
	}

	private Task ReloadAsync() => _loadTask = ReloadCoreAsync();

	private async Task ReloadCoreAsync()
	{
		var version = ++_loadVersion;
		var replyingToId = ReplyingTo?.Id;
		IsLoading = true;
		HasError = false;
		IsDisabled = false;
		HasMorePages = false;
		_currentPage = 0;
		Comments.Clear();

		try
		{
			var page = await FetchTopLevelAsync(1);
			if (version != _loadVersion)
			{
				return;
			}

			TotalCount = page.TotalCount;
			AppendPage(page);
			ReplyingTo = replyingToId is { } id ? Find(id) : null;
		}
		catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
		{
			if (version == _loadVersion)
			{
				IsDisabled = true;
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Loading comments failed");
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
			}
		}
	}

	[RelayCommand]
	private async Task LoadMoreAsync()
	{
		if (!CanLoadMore)
		{
			return;
		}

		var version = _loadVersion;
		IsLoadingMore = true;
		try
		{
			var page = await FetchTopLevelAsync(_currentPage + 1);
			if (version == _loadVersion)
			{
				AppendPage(page);
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Loading more comments failed");
			await ShowErrorAsync(ex, "CouldNotLoadData");
		}
		finally
		{
			IsLoadingMore = false;
		}
	}

	private void AppendPage(PagedResponse<CommentDto> page)
	{
		foreach (var dto in page.Data)
		{
			if (Comments.All(c => c.Id != dto.Id))
			{
				Comments.Add(new CommentItemViewModel(dto));
			}
		}

		_currentPage = page.PageNumber;
		HasMorePages = page.PageNumber * CommentLimits.PageSize < page.TotalCount;
		OnPropertyChanged(nameof(IsEmpty));
	}

	// An expired session must not turn a public read into an error, so it falls back to anonymous.
	private async Task<PagedResponse<CommentDto>> FetchTopLevelAsync(int pageNumber)
	{
		if (_userService.IsLoggedIn)
		{
			try
			{
				var authenticated = await _api.GetPostCommentsAuthenticatedAsync(_postId, Sort, pageNumber);
				await authenticated.EnsureSuccessfulAsync();
				return authenticated.Content!;
			}
			catch (AuthenticationFailedException)
			{
			}
		}

		var response = await _api.GetPostCommentsAsync(_postId, Sort, pageNumber);
		await response.EnsureSuccessfulAsync();
		return response.Content!;
	}

	private async Task<PagedResponse<CommentDto>> FetchRepliesAsync(Guid commentId, int pageNumber)
	{
		if (_userService.IsLoggedIn)
		{
			try
			{
				var authenticated = await _api.GetCommentRepliesAuthenticatedAsync(commentId, pageNumber);
				await authenticated.EnsureSuccessfulAsync();
				return authenticated.Content!;
			}
			catch (AuthenticationFailedException)
			{
			}
		}

		var response = await _api.GetCommentRepliesAsync(commentId, pageNumber);
		await response.EnsureSuccessfulAsync();
		return response.Content!;
	}

	public void SetSort(CommentSortOrder sort) => Sort = sort;

	public async Task LoadMoreRepliesAsync(CommentItemViewModel comment)
	{
		if (comment.IsBusy || !comment.HasMoreReplies)
		{
			return;
		}

		comment.IsBusy = true;
		try
		{
			var page = await FetchRepliesAsync(comment.Id, comment.NextRepliesPage);
			var isLast = page.PageNumber * CommentLimits.RepliesPageSize >= page.TotalCount;
			comment.MergeReplies(page.Data, isLast);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Loading replies failed");
			await ShowErrorAsync(ex, "CouldNotLoadData");
		}
		finally
		{
			comment.IsBusy = false;
		}
	}

	private bool CanPost() => !IsPosting && !string.IsNullOrWhiteSpace(DraftText);

	[RelayCommand(CanExecute = nameof(CanPost))]
	private async Task PostAsync()
	{
		if (IsDraftTooLong)
		{
			await _dialogService.ShowStatusMessageAsync(
				StatusMessageDialogType.Warning,
				Localizer.Instance.GetString("CommentPostFailed"),
				string.Format(CultureInfo.CurrentCulture, Localizer.Instance.GetString("CommentTooLongFormat"), CommentLimits.MaxCommentLength));
			return;
		}

		var content = DraftText.Trim();
		var parentId = ReplyingTo?.Id;
		var postId = _postId;

		IsPosting = true;
		try
		{
			// Write-then-auth: the draft stays in the box while the sign-in dialog is up.
			if (!await _signInPrompt.EnsureSignedInAsync())
			{
				return;
			}

			var (success, created) = await TryWriteAsync(
				() => _api.CreateCommentAsync(postId, new CreateCommentRequest { Content = content, ParentId = parentId }),
				"CommentPostFailed");
			if (!success || created is null || postId != _postId)
			{
				return;
			}

			// A reload kicked off by the sign-in has to land before the new comment is placed.
			await _loadTask;
			InsertCreated(created);
			DraftText = "";
			ReplyingTo = null;
		}
		finally
		{
			IsPosting = false;
		}
	}

	private void InsertCreated(CommentDto created)
	{
		if (Find(created.Id) is not null)
		{
			return;
		}

		CommentItemViewModel item = new(created);
		if (created.ParentId is { } parentId)
		{
			Comments.FirstOrDefault(c => c.Id == parentId)?.AddReply(item);
		}
		else
		{
			Comments.Insert(0, item);
			TotalCount++;
			OnPropertyChanged(nameof(IsEmpty));
		}
	}

	[RelayCommand]
	private void CancelReply() => ReplyingTo = null;

	public void BeginReply(CommentItemViewModel comment)
	{
		if (comment.CanReply)
		{
			ReplyingTo = comment;
		}
	}

	public async Task ToggleUpvoteAsync(CommentItemViewModel comment)
	{
		if (comment.IsBusy)
		{
			return;
		}

		if (!await _signInPrompt.EnsureSignedInAsync())
		{
			return;
		}

		// Signing in reloads the list, which replaces the item the click came from.
		await _loadTask;
		if (Find(comment.Id) is not { } target || target.IsBusy)
		{
			return;
		}

		var wasUpvoted = target.HasUpvoted;
		var previousCount = target.UpvoteCount;
		target.IsBusy = true;
		target.HasUpvoted = !wasUpvoted;
		target.UpvoteCount = Math.Max(0, previousCount + (wasUpvoted ? -1 : 1));
		try
		{
			var (success, updated) = await TryWriteAsync(
				() => wasUpvoted ? _api.RemoveCommentVoteAsync(target.Id) : _api.AddCommentVoteAsync(target.Id),
				"CommentActionFailed");
			if (success && updated is not null)
			{
				target.Apply(updated);
			}
			else
			{
				target.HasUpvoted = wasUpvoted;
				target.UpvoteCount = previousCount;
			}
		}
		finally
		{
			target.IsBusy = false;
		}
	}

	public void BeginEdit(CommentItemViewModel comment)
	{
		if (!comment.IsOwn)
		{
			return;
		}

		comment.EditText = comment.Item.Content;
		comment.IsEditing = true;
	}

	public void CancelEdit(CommentItemViewModel comment) => comment.IsEditing = false;

	public async Task SaveEditAsync(CommentItemViewModel comment)
	{
		var content = comment.EditText.Trim();
		if (comment.IsBusy || content.Length == 0 || content.Length > CommentLimits.MaxCommentLength)
		{
			return;
		}

		comment.IsBusy = true;
		try
		{
			var (success, updated) = await TryWriteAsync(
				() => _api.UpdateCommentAsync(comment.Id, new UpdateCommentRequest { Content = content }),
				"CommentActionFailed");
			if (success && updated is not null)
			{
				comment.Apply(updated);
				comment.IsEditing = false;
			}
		}
		finally
		{
			comment.IsBusy = false;
		}
	}

	public async Task DeleteAsync(CommentItemViewModel comment)
	{
		if (comment.IsBusy || !comment.IsOwn)
		{
			return;
		}

		var confirmation = await _dialogService.ShowConfirmationDialogAsync(
			Localizer.Instance.GetString("CommentDeleteTitle"),
			Localizer.Instance.GetString("CommentDeleteConfirm"));
		if (confirmation != ContentDialogResult.Primary)
		{
			return;
		}

		comment.IsBusy = true;
		try
		{
			var (success, _) = await TryWriteAsync(() => _api.DeleteCommentAsync(comment.Id), "CommentActionFailed");
			if (!success)
			{
				return;
			}

			if (comment.IsReply)
			{
				Comments.FirstOrDefault(c => c.Id == comment.ParentId)?.RemoveReply(comment);
			}
			else if (comment.Item.ReplyCount > 0)
			{
				comment.Apply(comment.Item with { Status = CommentStatus.DeletedByAuthor, Content = "", ContentHtml = "" });
			}
			else
			{
				Comments.Remove(comment);
				TotalCount = Math.Max(0, TotalCount - 1);
				OnPropertyChanged(nameof(IsEmpty));
			}
		}
		finally
		{
			comment.IsBusy = false;
		}
	}

	public async Task ReportAsync(CommentItemViewModel comment)
	{
		if (!await _signInPrompt.EnsureSignedInAsync())
		{
			return;
		}

		ReportCommentDialogViewModel dialogViewModel = new();
		if (await _dialogService.ShowAsync(dialogViewModel) != ContentDialogResult.Primary)
		{
			return;
		}

		var (success, _) = await TryWriteAsync(
			() => _api.ReportCommentAsync(comment.Id, new ReportCommentRequest { Reason = dialogViewModel.Reason, Note = dialogViewModel.NoteOrNull }),
			"CommentActionFailed");
		if (success)
		{
			await _dialogService.ShowStatusMessageAsync(
				StatusMessageDialogType.Information,
				Localizer.Instance.GetString("CommentReportedTitle"),
				Localizer.Instance.GetString("CommentReportedText"));
		}
	}

	private CommentItemViewModel? Find(Guid id)
	{
		foreach (var comment in Comments)
		{
			if (comment.Id == id)
			{
				return comment;
			}

			var reply = comment.Replies.FirstOrDefault(r => r.Id == id);
			if (reply is not null)
			{
				return reply;
			}
		}

		return null;
	}

	/// <summary>
	/// Runs a write call. A rejected or missing session re-prompts sign-in once; any other failure
	/// is logged and shown, and the call reports <c>Success: false</c>.
	/// </summary>
	private Task<(bool Success, T? Content)> TryWriteAsync<T>(Func<Task<ApiResponse<T>>> call, string failureTitleKey) =>
		TryRunAsync(
			async () =>
			{
				var response = await call();
				await response.EnsureSuccessfulAsync();
				return response.Content;
			},
			failureTitleKey);

	private Task<(bool Success, object? Content)> TryWriteAsync(Func<Task<IApiResponse>> call, string failureTitleKey) =>
		TryRunAsync<object?>(
			async () =>
			{
				var response = await call();
				if (response.Error is { } error)
				{
					throw error;
				}
				return null;
			},
			failureTitleKey);

	private async Task<(bool Success, T? Content)> TryRunAsync<T>(Func<Task<T?>> action, string failureTitleKey)
	{
		for (var attempt = 0; ; attempt++)
		{
			try
			{
				return (true, await action());
			}
			catch (Exception ex) when (attempt == 0 && IsUnauthorized(ex))
			{
				if (!await _signInPrompt.EnsureSignedInAsync())
				{
					return (false, default);
				}
			}
			catch (Exception ex) when (ex is ApiException or HttpRequestException or AuthenticationFailedException or TaskCanceledException)
			{
				_logger.LogError(ex, "A comment request failed");
				await ShowErrorAsync(ex, failureTitleKey);
				return (false, default);
			}
		}
	}

	private static bool IsUnauthorized(Exception ex) =>
		ex is AuthenticationFailedException || ex is ApiException { StatusCode: HttpStatusCode.Unauthorized };

	private async Task ShowErrorAsync(Exception ex, string titleKey) =>
		await _dialogService.ShowStatusMessageAsync(
			StatusMessageDialogType.Error,
			Localizer.Instance.GetString(titleKey),
			Describe(ex));

	private static string Describe(Exception ex)
	{
		if (ex is AuthenticationFailedException)
		{
			return ex.Message;
		}

		if (ex is not ApiException api)
		{
			return Localizer.Instance.GetString("CommentGenericError");
		}

		if (api.StatusCode == HttpStatusCode.TooManyRequests)
		{
			return api.Headers?.RetryAfter?.Delta is { } wait && wait > TimeSpan.Zero ?
				string.Format(CultureInfo.CurrentCulture, Localizer.Instance.GetString("CommentSlowDownWaitFormat"), wait.Humanize()) :
				Localizer.Instance.GetString("CommentSlowDown");
		}

		if (api.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Forbidden or HttpStatusCode.NotFound or HttpStatusCode.Conflict
			&& ReadProblemDetail(api.Content) is { } detail)
		{
			return detail;
		}

		return Localizer.Instance.GetString(api.StatusCode == HttpStatusCode.Forbidden ? "CommentBanned" : "CommentGenericError");
	}

	private static string? ReadProblemDetail(string? body)
	{
		if (string.IsNullOrWhiteSpace(body))
		{
			return null;
		}

		try
		{
			using var document = JsonDocument.Parse(body);
			if (document.RootElement.ValueKind == JsonValueKind.Object &&
				document.RootElement.TryGetProperty("detail", out var detail) &&
				detail.ValueKind == JsonValueKind.String &&
				!string.IsNullOrWhiteSpace(detail.GetString()))
			{
				return detail.GetString();
			}
		}
		catch (JsonException)
		{
		}

		return null;
	}
}
