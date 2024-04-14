namespace MZikmund.Services.Preferences;

public class AppPreferences : IAppPreferences
{
	private const string AppThemeKey = "AppTheme";
	private const string AccessTokenKey = "AccessToken";

	private readonly IPreferencesService _preferences;

	public AppPreferences(IPreferencesService preferences)
	{
		_preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
	}

	public AppTheme Theme
	{
		get => _preferences.GetComplex(AppThemeKey, () => AppTheme.System, true);
		set => _preferences.SetComplex(AppThemeKey, value, true);
	}

	public string? AccessToken
	{
		get => _preferences.GetSetting(AccessTokenKey, () => (string?)null, true);
		set => _preferences.SetSetting(AccessTokenKey, value, true);
	}
}
