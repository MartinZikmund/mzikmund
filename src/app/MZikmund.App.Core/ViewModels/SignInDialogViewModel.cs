using MZikmund.Services.Account;
using MZikmund.Services.Localization;
using MZikmund.Shared.Authentication;

namespace MZikmund.ViewModels;

public partial class SignInDialogViewModel : DialogViewModel
{
	private readonly IUserService _userService;
	private readonly CancellationTokenSource _cancellation = new();
	private ContentDialog? _dialog;

	public SignInDialogViewModel(IUserService userService, string? webUrl)
	{
		_userService = userService ?? throw new ArgumentNullException(nameof(userService));
		PrivacyUri = webUrl is null ? null : new Uri(new Uri(webUrl.TrimEnd('/') + "/"), "privacy");
	}

	public override string Title => Localizer.Instance.GetString("SignInTitle");

	public Uri? PrivacyUri { get; }

	public string CodePrompt => string.Format(
		System.Globalization.CultureInfo.CurrentCulture,
		Localizer.Instance.GetString("SignInCodePromptFormat"),
		Email.Trim());

	public bool IsSignedIn => _userService.IsLoggedIn;

	public bool IsChooseStep => Step == SignInStep.Choose;

	public bool IsEmailStep => Step == SignInStep.Email;

	public bool IsCodeStep => Step == SignInStep.Code;

	public bool IsNotBusy => !IsBusy;

	public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsChooseStep))]
	[NotifyPropertyChangedFor(nameof(IsEmailStep))]
	[NotifyPropertyChangedFor(nameof(IsCodeStep))]
	public partial SignInStep Step { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(CodePrompt))]
	public partial string Email { get; set; } = "";

	[ObservableProperty]
	public partial string Code { get; set; } = "";

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsNotBusy))]
	public partial bool IsBusy { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasError))]
	public partial string? ErrorMessage { get; set; }

	public override void OnOpened(ContentDialog contentDialog)
	{
		_dialog = contentDialog;
		// Close, Escape and a finished sign-in all end here; a browser wait still running must stop.
		contentDialog.Closed += (_, _) => _cancellation.Cancel();
	}

	[RelayCommand]
	private Task SignInWithGoogleAsync() => SignInWithProviderAsync(SupabaseOAuthProvider.Google);

	[RelayCommand]
	private Task SignInWithAppleAsync() => SignInWithProviderAsync(SupabaseOAuthProvider.Apple);

	[RelayCommand]
	private void ContinueWithEmail()
	{
		ErrorMessage = null;
		Step = SignInStep.Email;
	}

	[RelayCommand]
	private void UseDifferentEmail()
	{
		ErrorMessage = null;
		Code = "";
		Step = SignInStep.Email;
	}

	[RelayCommand]
	private Task SendCodeAsync() => RunAsync(async () =>
	{
		await _userService.SendEmailCodeAsync(Email);
		Code = "";
		Step = SignInStep.Code;
	});

	[RelayCommand]
	private Task VerifyCodeAsync() => RunAsync(async () =>
	{
		await _userService.VerifyEmailCodeAsync(Email, Code);
		OnSignedIn();
	});

	private Task SignInWithProviderAsync(SupabaseOAuthProvider provider) => RunAsync(async () =>
	{
		await _userService.SignInWithProviderAsync(provider, _cancellation.Token);
		if (_userService.IsLoggedIn)
		{
			OnSignedIn();
		}
	});

	private void OnSignedIn()
	{
		OnPropertyChanged(nameof(IsSignedIn));
		_dialog?.Hide();
	}

	private async Task RunAsync(Func<Task> action)
	{
		ErrorMessage = null;
		IsBusy = true;
		try
		{
			await action();
		}
		catch (AuthenticationFailedException ex)
		{
			ErrorMessage = ex.Message;
		}
		finally
		{
			IsBusy = false;
		}
	}
}

public enum SignInStep
{
	Choose,
	Email,
	Code,
}
