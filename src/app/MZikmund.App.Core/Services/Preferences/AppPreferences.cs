namespace MZikmund.Services.Preferences;

public class AppPreferences : IAppPreferences
{
	private const string AppThemeKey = "AppTheme";
	private const string AccessTokenKey = "SessionAccessToken";
	private const string RefreshTokenKey = "SessionRefreshToken";
	// Tokens used to be roamed, which copies them to the user's other devices.
	private const string LegacyAccessTokenKey = "AccessToken";
	private const string LegacyRefreshTokenKey = "RefreshToken";
	private const string UserIdKey = "UserId";
	private const string DisplayNameKey = "DisplayName";
	private const string TokenExpiresOnKey = "TokenExpiresOn";
	private const string CommentsLastSeenOnKey = "CommentsLastSeenOn";

	private readonly IPreferencesService _preferences;

	public AppPreferences(IPreferencesService preferences)
	{
		_preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
		MigrateRoamedTokens();
	}

	public AppTheme Theme
	{
		get => _preferences.GetComplex(AppThemeKey, () => AppTheme.System, true);
		set => _preferences.SetComplex(AppThemeKey, value, true);
	}

	public string? AccessToken
	{
		get => _preferences.GetSetting(AccessTokenKey, () => (string?)null);
		set => _preferences.SetSetting(AccessTokenKey, value);
	}

	public string? RefreshToken
	{
		get => _preferences.GetSetting(RefreshTokenKey, () => (string?)null);
		set => _preferences.SetSetting(RefreshTokenKey, value);
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

	public DateTimeOffset? CommentsLastSeenOn
	{
		get => _preferences.GetComplex(CommentsLastSeenOnKey, () => (DateTimeOffset?)null, true);
		set => _preferences.SetComplex(CommentsLastSeenOnKey, value, true);
	}

	// Copies a previously roamed session to local storage once, then removes the roamed copy.
	private void MigrateRoamedTokens()
	{
		var legacyAccess = _preferences.GetSetting(LegacyAccessTokenKey, () => (string?)null, true);
		var legacyRefresh = _preferences.GetSetting(LegacyRefreshTokenKey, () => (string?)null, true);
		if (legacyAccess is null && legacyRefresh is null)
		{
			return;
		}

		if (AccessToken is null && RefreshToken is null)
		{
			AccessToken = legacyAccess;
			RefreshToken = legacyRefresh;
		}

		_preferences.SetSetting<string?>(LegacyAccessTokenKey, null, true);
		_preferences.SetSetting<string?>(LegacyRefreshTokenKey, null, true);
	}
}
