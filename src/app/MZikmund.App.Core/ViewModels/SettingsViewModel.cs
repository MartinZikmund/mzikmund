using MZikmund.Services.Preferences;
using MZikmund.Services.Theming;
using System;
using System.Linq;
using MZikmund.ViewModels;
using MZikmund.Services.Localization;
using Windows.ApplicationModel;

namespace MZikmund.ViewModels;

public class SettingsViewModel : PageViewModel
{
	private readonly IAppPreferences _appPreferences;
	private readonly IThemeManager _themeManager;

	public SettingsViewModel(IAppPreferences appPreferences, IThemeManager themeManager)
	{
		_appPreferences = appPreferences ?? throw new ArgumentNullException(nameof(appPreferences));
		_themeManager = themeManager ?? throw new ArgumentNullException(nameof(themeManager));
	}

	public override string Title => Localizer.Instance.GetString("Settings");

	public AppTheme[] Themes { get; } = Enum.GetValues(typeof(AppTheme)).OfType<AppTheme>().ToArray();

	public string Version
	{
		get
		{
			var packageVersion = Package.Current.Id.Version;
			return $"{packageVersion.Major}.{packageVersion.Minor}.{packageVersion.Build}.{packageVersion.Revision}";
		}
	}

	public object SelectedTheme
	{
		get => _appPreferences.Theme;
		set
		{
			var theme = value as AppTheme?;
			if (theme == null)
			{
				return;
			}

			if (theme.Value != _appPreferences.Theme)
			{
				_appPreferences.Theme = theme.Value;
				_themeManager.SetTheme(theme.Value);
				OnPropertyChanged();
			}
		}
	}
}
