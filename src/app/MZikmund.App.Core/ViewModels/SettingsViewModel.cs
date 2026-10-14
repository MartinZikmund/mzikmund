using System.Reflection;
using MZikmund.Services.Account;
using MZikmund.Services.Localization;
using MZikmund.Services.Preferences;
using MZikmund.Services.Theming;
using Launcher = Windows.System.Launcher;

namespace MZikmund.ViewModels;

public partial class SettingsViewModel : PageViewModel
{
	private static readonly AppTheme[] ThemeOrder = [AppTheme.System, AppTheme.Light, AppTheme.Dark];

	private readonly IAppPreferences _appPreferences;
	private readonly IThemeManager _themeManager;
	private readonly IUserService _userService;

	public SettingsViewModel(IAppPreferences appPreferences, IThemeManager themeManager, IUserService userService)
	{
		_appPreferences = appPreferences ?? throw new ArgumentNullException(nameof(appPreferences));
		_themeManager = themeManager ?? throw new ArgumentNullException(nameof(themeManager));
		_userService = userService ?? throw new ArgumentNullException(nameof(userService));
	}

	public override string Title => Localizer.Instance.GetString("Settings");

	public string Version
	{
		get
		{
			var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
			var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

			// Nerdbank appends the commit after a '+'; the short form is enough for a settings page.
			return string.IsNullOrEmpty(informationalVersion) ?
				assembly.GetName().Version?.ToString() ?? "1.0.0" :
				informationalVersion.Split('+')[0];
		}
	}

	public int SelectedThemeIndex
	{
		get => Array.IndexOf(ThemeOrder, _appPreferences.Theme);
		set
		{
			if (value < 0 || value >= ThemeOrder.Length || ThemeOrder[value] == _appPreferences.Theme)
			{
				return;
			}

			_appPreferences.Theme = ThemeOrder[value];
			_themeManager.SetTheme(ThemeOrder[value]);
			OnPropertyChanged();
		}
	}

	public string AccountDescription => _userService.IsLoggedIn ?
		string.Format(System.Globalization.CultureInfo.CurrentCulture, Localizer.Instance.GetString("SignedInAsFormat"), _userService.UserName) :
		Localizer.Instance.GetString("NotSignedIn");

	public override void ViewLoaded()
	{
		base.ViewLoaded();
		OnPropertyChanged(nameof(AccountDescription));
	}

	[RelayCommand]
	private static async Task OpenLinkAsync(string? url)
	{
		if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
		{
			await Launcher.LaunchUriAsync(uri);
		}
	}
}
