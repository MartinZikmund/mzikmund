using Microsoft.Identity.Client;
using Uno.UI.MSAL;
using MZikmund.Services.Preferences;

namespace MZikmund.Services.Account;

public class UserService : IUserService
{
	private const string CLIENT_ID = "e65fdc12-c11b-4300-99c3-e1129215e049";
	private const string TENANT_ID = "4e973842-1a98-40ec-9542-3c2019f0fb8e";

#if __WASM__
	private const string REDIRECT_URI = "http://localhost:51407/authentication/login-callback.htm";
#elif __IOS__ || __MACOS__
	private const string REDIRECT_URI = "msal" + CLIENT_ID + "://auth";
#elif __ANDROID__
	private const string REDIRECT_URI = "msauth://Uno.MSAL.Graph.Demo/BUWXtvbCbxw6rdZidSYhNH6gLvA%3D";
#else
	private const string REDIRECT_URI = "https://login.microsoftonline.com/common/oauth2/nativeclient";
#endif

	private readonly IPublicClientApplication _publicClientApp;
	private readonly IAppPreferences _preferences;

	private readonly HttpClient _client = new HttpClient();
	private readonly string[] _scopes = new[]
	{
			"user.read",
			"api://mzikmund/access_as_user"
		};

	public UserService(IAppPreferences preferences)
	{
		_preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));

		_publicClientApp = PublicClientApplicationBuilder
			.Create(CLIENT_ID)
			.WithTenantId(TENANT_ID)
			.WithRedirectUri(REDIRECT_URI)
			.WithUnoHelpers()
			.Build();
	}

	public bool IsLoggedIn => AccessToken != null;

	public string? AccessToken => _preferences.AccessToken;

	public async Task AuthenticateAsync()
	{
		var authResult = await _publicClientApp
			.AcquireTokenInteractive(_scopes)
			.WithUnoHelpers()
			.ExecuteAsync();

		_preferences.AccessToken = authResult.AccessToken;
	}
}
