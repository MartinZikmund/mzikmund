namespace MZikmund.Services.Preferences;

public class AppPreferences : IAppPreferences
{
	private const string AppThemeKey = "AppTheme";
	private const string AccessTokenKey = "AccessToken";
	private const string RefreshTokenKey = "RefreshToken";
	private const string UserIdKey = "UserId";
	private const string DisplayNameKey = "DisplayName";
	private const string TokenExpiresOnKey = "TokenExpiresOn";

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

	public string? RefreshToken
	{
		get => _preferences.GetSetting(RefreshTokenKey, () => (string?)null, true);
		set => _preferences.SetSetting(RefreshTokenKey, value, true);
	}

	public string? UserId
	{
		get => _preferences.GetSetting(UserIdKey, () => (string?)null, true);
		set => _preferences.SetSetting(UserIdKey, value, true);
	}

	public string? DisplayName
	{
		get => _preferences.GetSetting(DisplayNameKey, () => (string?)null, true);
		set => _preferences.SetSetting(DisplayNameKey, value, true);
	}

	public DateTimeOffset? TokenExpiresOn
	{
		get => _preferences.GetComplex(TokenExpiresOnKey, () => (DateTimeOffset?)null, true);
		set => _preferences.SetComplex(TokenExpiresOnKey, value, true);
	}
}
