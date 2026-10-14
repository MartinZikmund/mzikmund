using Microsoft.UI.Dispatching;
using MZikmund.Models.Dialogs;
using MZikmund.Services.Account;
using MZikmund.Services.Comments;
using MZikmund.Services.Dialogs;
using MZikmund.Services.Localization;
using MZikmund.Services.Navigation;
using MZikmund.Shared.Blog;
using Uno.Disposables;

namespace MZikmund.ViewModels;

public partial class WindowShellViewModel : ViewModelBase
{
	private readonly IWindowShellProvider _provider;
	private readonly INavigationService _navigationService;
	private readonly IUserService _userService;
	private readonly IDialogService _dialogService;
	private readonly ISignInPrompt _signInPrompt;
	private readonly ICommentsBadgeService _commentsBadge;
	private RefCountDisposable? _refCountDisposable;

	public WindowShellViewModel(IWindowShellProvider provider, INavigationService navigationService, IUserService userService, IDialogService dialogService, ISignInPrompt signInPrompt, ICommentsBadgeService commentsBadge)
	{
		_commentsBadge = commentsBadge ?? throw new ArgumentNullException(nameof(commentsBadge));
		_signInPrompt = signInPrompt ?? throw new ArgumentNullException(nameof(signInPrompt));
		_dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
		_provider = provider ?? throw new ArgumentNullException(nameof(provider));
		_navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
		_userService = userService ?? throw new ArgumentNullException(nameof(userService));

		// Initialize from cached authentication state
		UpdateAuthenticationState();
		_userService.AuthenticationChanged += OnAuthenticationChanged;
		_commentsBadge.Changed += OnCommentsBadgeChanged;
	}

	public string Title { get; } = "Martin Zikmund";

	[ObservableProperty]
	public partial bool CanGoBack { get; set; }

	[ObservableProperty]
	public partial string? UserName { get; set; }

	[ObservableProperty]
	public partial bool IsLoading { get; set; }

	[ObservableProperty]
	public partial string LoadingStatusMessage { get; set; } = "";

	[ObservableProperty]
	public partial bool IsLoggedIn { get; set; }

	[ObservableProperty]
	public partial bool IsAdmin { get; set; }

	[ObservableProperty]
	public partial string UnreadCommentsBadge { get; set; } = "";

	[ObservableProperty]
	public partial int UnreadCommentsValue { get; set; }

	[ObservableProperty]
	public partial bool HasUnreadComments { get; set; }

	/// <summary>
	/// Re-reads the unread count; the shell calls it when the Admin menu is opened.
	/// </summary>
	public void RefreshCommentsBadge()
	{
		if (IsAdmin)
		{
			_ = _commentsBadge.RefreshAsync();
		}
	}

	[RelayCommand]
	private async Task LoginAsync()
	{
		try
		{
			await _signInPrompt.EnsureSignedInAsync();
		}
		catch (AuthenticationFailedException ex)
		{
			await _dialogService.ShowStatusMessageAsync(
				StatusMessageDialogType.Error,
				Localizer.Instance.GetString("SignInFailed"),
				ex.Message);
		}

		UpdateAuthenticationState();
	}

	[RelayCommand]
	private async Task LogoutAsync()
	{
		await _userService.LogoutAsync();
		UpdateAuthenticationState();
		_navigationService.Navigate<HomeViewModel>();
		_navigationService.ClearBackStack();
	}

	// Raised wherever a token was needed, which can be a thread-pool continuation of an API call.
	private void OnAuthenticationChanged(object? sender, EventArgs e)
	{
		if (_provider.DispatcherQueue.HasThreadAccess)
		{
			UpdateAuthenticationState();
		}
		else
		{
			_provider.DispatcherQueue.TryEnqueue(UpdateAuthenticationState);
		}
	}

	private void OnCommentsBadgeChanged(object? sender, EventArgs e)
	{
		if (_provider.DispatcherQueue.HasThreadAccess)
		{
			UpdateCommentsBadge();
		}
		else
		{
			_provider.DispatcherQueue.TryEnqueue(UpdateCommentsBadge);
		}
	}

	private void UpdateCommentsBadge()
	{
		HasUnreadComments = IsAdmin && _commentsBadge.UnreadCount > 0;
		UnreadCommentsValue = _commentsBadge.UnreadCount;
		UnreadCommentsBadge = CommentDisplay.FormatBadgeCount(_commentsBadge.UnreadCount);
	}

	private void UpdateAuthenticationState()
	{
		var wasAdmin = IsAdmin;
		IsLoggedIn = _userService.IsLoggedIn;
		IsAdmin = _userService.IsAdmin;
		UserName = _userService.UserName;

		if (IsAdmin && !wasAdmin)
		{
			RefreshCommentsBadge();
		}
		else if (!IsAdmin)
		{
			UpdateCommentsBadge();
		}
	}

	public IDisposable BeginLoading()
	{
		LoadingStatusMessage = "";
		if (_refCountDisposable != null && !_refCountDisposable.IsDisposed)
		{
			return _refCountDisposable.GetDisposable();
		}

		IsLoading = true;
		_refCountDisposable = new RefCountDisposable(Disposable.Create(
			() => // TODO: Await TryEnequeAsync
			{
#if __WASM__
				IsLoading = false;
				return;
#else
				if (_provider.DispatcherQueue.HasThreadAccess)
				{
					IsLoading = false;
				}
				else
				{
					_provider.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
					{
						if (_refCountDisposable == null || _refCountDisposable.IsDisposed)
						{
							IsLoading = false;
						}
					});
				}
#endif
			}));
		return _refCountDisposable;
	}

}
