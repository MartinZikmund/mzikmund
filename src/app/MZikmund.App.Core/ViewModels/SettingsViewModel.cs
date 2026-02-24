using System;
using System.Linq;
using System.Reflection;
using MZikmund.Services.Localization;
using MZikmund.Services.Preferences;
using MZikmund.Services.Theming;
using MZikmund.ViewModels;
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
			// Get the assembly informational version
			var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
			var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

			if (string.IsNullOrEmpty(informationalVersion))
			{
				var version = assembly.GetName().Version;
				return version?.ToString() ?? "1.0.0";
			}
			else
			{
				return informationalVersion;
			}
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
