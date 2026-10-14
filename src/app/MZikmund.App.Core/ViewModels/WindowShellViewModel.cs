using Microsoft.UI.Dispatching;
using MZikmund.Models.Dialogs;
using MZikmund.Services.Account;
using MZikmund.Services.Dialogs;
using MZikmund.Services.Localization;
using MZikmund.Services.Navigation;
using Uno.Disposables;

namespace MZikmund.ViewModels;

public partial class WindowShellViewModel : ViewModelBase
{
	private readonly IWindowShellProvider _provider;
	private readonly INavigationService _navigationService;
	private readonly IUserService _userService;
	private readonly IDialogService _dialogService;
	private RefCountDisposable? _refCountDisposable;

	public WindowShellViewModel(IWindowShellProvider provider, INavigationService navigationService, IUserService userService, IDialogService dialogService)
	{
		_dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
		_provider = provider ?? throw new ArgumentNullException(nameof(provider));
		_navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
		_userService = userService ?? throw new ArgumentNullException(nameof(userService));

		// Initialize from cached authentication state
		UpdateAuthenticationState();
	}

	public string Title { get; set; } = "Martin Zikmund";

	[ObservableProperty]
	public partial bool IsLoading { get; set; }

	[ObservableProperty]
	public partial string LoadingStatusMessage { get; set; } = "";

	[ObservableProperty]
	public partial bool IsLoggedIn { get; set; }

	[RelayCommand]
	private async Task LoginAsync()
	{
		try
		{
			await _userService.AuthenticateAsync();
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
		_navigationService.Navigate<BlogViewModel>();
	}

	private void UpdateAuthenticationState()
	{
		IsLoggedIn = _userService.IsLoggedIn;
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


	public void BackRequested() => _navigationService.GoBack();
}
