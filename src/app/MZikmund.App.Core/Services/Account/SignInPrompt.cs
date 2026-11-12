using Microsoft.Extensions.Options;
using MZikmund.Api.Client;
using MZikmund.Business.Models;
using MZikmund.Services.Dialogs;
using MZikmund.ViewModels;

namespace MZikmund.Services.Account;

public class SignInPrompt : ISignInPrompt
{
	private readonly IDialogService _dialogService;
	private readonly IUserService _userService;
	private readonly IMZikmundApi _api;
	private readonly IOptions<AppConfig> _appConfig;
	private Task<bool>? _pending;

	public SignInPrompt(IDialogService dialogService, IUserService userService, IMZikmundApi api, IOptions<AppConfig> appConfig)
	{
		_dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
		_userService = userService ?? throw new ArgumentNullException(nameof(userService));
		_api = api ?? throw new ArgumentNullException(nameof(api));
		_appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
	}

	public Task<bool> EnsureSignedInAsync() =>
		_userService.IsLoggedIn ? Task.FromResult(true) : _pending ??= ShowDialogAsync();

	private async Task<bool> ShowDialogAsync()
	{
		try
		{
			await _dialogService.ShowAsync(new SignInDialogViewModel(_userService, _api, _appConfig.Value.WebUrl));
			return _userService.IsLoggedIn;
		}
		finally
		{
			_pending = null;
		}
	}
}
