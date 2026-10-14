using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Microsoft.UI;
using MZikmund.App.Core.Infrastructure;
using MZikmund.Services.Preferences;
using MZikmund.Shared.Authentication;

#if WINDOWS
using Microsoft.Security.Authentication.OAuth;
#elif __DESKTOP__
using System.Net;
using System.Text;
#else
using Windows.Security.Authentication.Web;
#endif

namespace MZikmund.Services.Account;

public class UserService : IUserService
{
	private static readonly TimeSpan SignInTimeout = TimeSpan.FromMinutes(5);

	private AuthenticationInfo? _authenticationInfo;
	private string? _refreshToken;
	private readonly IAppPreferences _appPreferences;
	private readonly IOptions<AuthConfig> _authConfig;
#if WINDOWS
	private readonly IApplication _application;
#endif

	public UserService(
#if WINDOWS
		IApplication application,
#endif
		IAppPreferences appPreferences,
		IOptions<AuthConfig> authConfig)
	{
#if WINDOWS
		_application = application;
#endif
		_appPreferences = appPreferences;
		_authConfig = authConfig;

		LoadCachedAuthentication();
	}

	public bool IsLoggedIn => _authenticationInfo != null;

	public string? UserName => _authenticationInfo?.DisplayName;

	public bool NeedsRefresh => _authenticationInfo?.ExpiresOn < DateTimeOffset.UtcNow.AddMinutes(5);

	public string? AccessToken => _authenticationInfo?.Token;

	private AuthConfig Config => _authConfig.Value;

	private string Authority => $"https://{Config.Domain}";

	private void LoadCachedAuthentication()
	{
		var accessToken = _appPreferences.AccessToken;
		var userId = _appPreferences.UserId;
		var displayName = _appPreferences.DisplayName;
		var expiresOn = _appPreferences.TokenExpiresOn;

		if (!string.IsNullOrEmpty(accessToken) &&
			!string.IsNullOrEmpty(userId) &&
			!string.IsNullOrEmpty(displayName) &&
			expiresOn.HasValue)
		{
			_authenticationInfo = new AuthenticationInfo
			{
				Token = accessToken,
				UserId = userId,
				DisplayName = displayName,
				ExpiresOn = expiresOn.Value
			};

			_refreshToken = _appPreferences.RefreshToken;
		}
	}

	private void SaveAuthenticationToCache()
	{
		if (_authenticationInfo != null)
		{
			_appPreferences.AccessToken = _authenticationInfo.Token;
			_appPreferences.UserId = _authenticationInfo.UserId;
			_appPreferences.DisplayName = _authenticationInfo.DisplayName;
			_appPreferences.TokenExpiresOn = _authenticationInfo.ExpiresOn;
			_appPreferences.RefreshToken = _refreshToken;
		}
	}

	private void ClearAuthenticationCache()
	{
		_appPreferences.AccessToken = null;
		_appPreferences.RefreshToken = null;
		_appPreferences.UserId = null;
		_appPreferences.DisplayName = null;
		_appPreferences.TokenExpiresOn = null;
	}

	public async Task AuthenticateAsync()
	{
		if (IsLoggedIn && !NeedsRefresh)
		{
			return;
		}

		if (!Config.IsConfigured)
		{
			throw new AuthenticationFailedException(
				"Sign-in is not configured. Set AuthConfig:ClientId (for example via the " +
				"MZIKMUND_AuthConfig__ClientId environment variable). See docs/AUTH0_MIGRATION.md.");
		}

		if (!string.IsNullOrEmpty(_refreshToken) && await TryRefreshTokenAsync())
		{
			SaveAuthenticationToCache();
			return;
		}

		_authenticationInfo = null;
		_refreshToken = null;
		ClearAuthenticationCache();

		await PerformAuthenticationAsync();
		SaveAuthenticationToCache();
	}

#if WINDOWS
	private async Task PerformAuthenticationAsync()
	{
		if (_application.MainWindow is null)
		{
			throw new InvalidOperationException("Window is not available");
		}

		var authRequestParams = AuthRequestParams.CreateForAuthorizationCodeRequest(
			Config.ClientId,
			new Uri("mzikmund-app://oauthcallback"));
		authRequestParams.Scope = string.Join(" ", AuthenticationConstants.DefaultScopes);

		var authUrl = $"{Authority}/authorize?audience={Uri.EscapeDataString(Config.Audience)}";

		var authResult = await OAuth2Manager.RequestAuthWithParamsAsync(
			GetWindowId(),
			new Uri(authUrl),
			authRequestParams);

		if (authResult.Failure is { } authFailure)
		{
			if (authFailure.Error == "user_cancelled")
			{
				return;
			}

			throw new AuthenticationFailedException($"{authFailure.Error}: {authFailure.ErrorDescription}");
		}

		var tokenResult = await OAuth2Manager.RequestTokenAsync(
			new Uri($"{Authority}/oauth/token"),
			TokenRequestParams.CreateForAuthorizationCodeRequest(authResult.Response));

		if (tokenResult.Failure is { } tokenFailure)
		{
			throw new AuthenticationFailedException($"{tokenFailure.Error}: {tokenFailure.ErrorDescription}");
		}

		var tokenResponse = tokenResult.Response;
		_refreshToken = tokenResponse.RefreshToken;
		_authenticationInfo = CreateAuthenticationInfo(tokenResponse.AccessToken, tokenResponse.ExpiresIn);
	}

	private async Task<bool> TryRefreshTokenAsync()
	{
		try
		{
			var tokenResult = await OAuth2Manager.RequestTokenAsync(
				new Uri($"{Authority}/oauth/token"),
				TokenRequestParams.CreateForRefreshToken(_refreshToken));

			if (tokenResult.Response is not { } tokenResponse)
			{
				Debug.WriteLine($"Token refresh failed: {tokenResult.Failure?.Error} - {tokenResult.Failure?.ErrorDescription}");
				return false;
			}

			if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
			{
				_refreshToken = tokenResponse.RefreshToken;
			}

			_authenticationInfo = CreateAuthenticationInfo(tokenResponse.AccessToken, tokenResponse.ExpiresIn);
			return true;
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Token refresh failed: {ex.Message}");
			return false;
		}
	}

	private WindowId GetWindowId()
	{
		var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(_application.MainWindow);
		return new WindowId((ulong)windowHandle);
	}
#else
	private async Task PerformAuthenticationAsync()
	{
		var codeVerifier = Pkce.CreateCodeVerifier();
		var state = Pkce.CreateState();
		var redirectUri = GetRedirectUri();

		var authUri = new Uri(
			$"{Authority}/authorize" +
			$"?response_type=code" +
			$"&client_id={Uri.EscapeDataString(Config.ClientId!)}" +
			$"&redirect_uri={Uri.EscapeDataString(redirectUri.AbsoluteUri)}" +
			$"&scope={Uri.EscapeDataString(string.Join(" ", AuthenticationConstants.DefaultScopes))}" +
			$"&audience={Uri.EscapeDataString(Config.Audience)}" +
			$"&code_challenge={Pkce.CreateCodeChallenge(codeVerifier)}" +
			$"&code_challenge_method=S256" +
			$"&state={state}");

		var callbackUri = await GetAuthorizationCallbackAsync(authUri, redirectUri);
		if (callbackUri is null)
		{
			return;
		}

		var callback = OAuthCallback.Parse(callbackUri, state);
		if (callback.Code is null)
		{
			throw new AuthenticationFailedException($"{callback.Error}: {callback.ErrorDescription}");
		}

		var tokenResponse = await RequestTokenAsync(new Dictionary<string, string>
		{
			["grant_type"] = "authorization_code",
			["client_id"] = Config.ClientId!,
			["code"] = callback.Code,
			["code_verifier"] = codeVerifier,
			["redirect_uri"] = redirectUri.AbsoluteUri,
		});

		_refreshToken = tokenResponse.RefreshToken;
		_authenticationInfo = CreateAuthenticationInfo(tokenResponse.AccessToken, tokenResponse.ExpiresIn);
	}

#if __DESKTOP__
	private Uri GetRedirectUri() => new($"http://127.0.0.1:{Config.DesktopCallbackPort}/callback");

	private static async Task<Uri?> GetAuthorizationCallbackAsync(Uri authUri, Uri redirectUri)
	{
		using HttpListener listener = new();
		listener.Prefixes.Add(redirectUri.GetLeftPart(UriPartial.Path).TrimEnd('/') + "/");
		listener.Start();

		await Windows.System.Launcher.LaunchUriAsync(authUri);

		var contextTask = listener.GetContextAsync();
		if (await Task.WhenAny(contextTask, Task.Delay(SignInTimeout)) != contextTask)
		{
			throw new AuthenticationFailedException("Sign-in timed out.");
		}

		var context = await contextTask;
		var page = Encoding.UTF8.GetBytes(
			"<html><body style=\"font-family:sans-serif;padding:48px\">" +
			"<h2>Signed in</h2><p>You can close this tab and return to the app.</p></body></html>");
		context.Response.ContentType = "text/html; charset=utf-8";
		context.Response.ContentLength64 = page.Length;
		await context.Response.OutputStream.WriteAsync(page);
		context.Response.Close();

		return context.Request.Url;
	}
#else
	private static Uri GetRedirectUri() => WebAuthenticationBroker.GetCurrentApplicationCallbackUri();

	private static async Task<Uri?> GetAuthorizationCallbackAsync(Uri authUri, Uri redirectUri)
	{
		var result = await WebAuthenticationBroker.AuthenticateAsync(WebAuthenticationOptions.None, authUri, redirectUri);
		return result.ResponseStatus switch
		{
			WebAuthenticationStatus.Success => new Uri(result.ResponseData),
			WebAuthenticationStatus.UserCancel => null,
			_ => throw new AuthenticationFailedException($"Sign-in failed (HTTP {result.ResponseErrorDetail})."),
		};
	}
#endif

	private async Task<bool> TryRefreshTokenAsync()
	{
		try
		{
			var tokenResponse = await RequestTokenAsync(new Dictionary<string, string>
			{
				["grant_type"] = "refresh_token",
				["client_id"] = Config.ClientId!,
				["refresh_token"] = _refreshToken!,
			});

			if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
			{
				_refreshToken = tokenResponse.RefreshToken;
			}

			_authenticationInfo = CreateAuthenticationInfo(tokenResponse.AccessToken, tokenResponse.ExpiresIn);
			return true;
		}
		catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or AuthenticationFailedException)
		{
			Debug.WriteLine($"Token refresh failed: {ex.Message}");
			return false;
		}
	}

	private async Task<TokenResponse> RequestTokenAsync(Dictionary<string, string> form)
	{
		using HttpClient httpClient = new();
		using var response = await httpClient.PostAsync($"{Authority}/oauth/token", new FormUrlEncodedContent(form));
		var content = await response.Content.ReadAsStringAsync();

		if (!response.IsSuccessStatusCode)
		{
			throw new AuthenticationFailedException($"Token request failed ({(int)response.StatusCode}): {content}");
		}

		return JsonSerializer.Deserialize<TokenResponse>(content) ??
			throw new AuthenticationFailedException("Token response was empty.");
	}

	private sealed class TokenResponse
	{
		[JsonPropertyName("access_token")]
		public string AccessToken { get; set; } = "";

		[JsonPropertyName("refresh_token")]
		public string? RefreshToken { get; set; }

		[JsonPropertyName("expires_in")]
		public int ExpiresIn { get; set; }
	}
#endif

	private static AuthenticationInfo CreateAuthenticationInfo(string accessToken, double expiresInSeconds)
	{
		var token = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
		return new AuthenticationInfo
		{
			DisplayName = token.Claims.FirstOrDefault(c => c.Type == "name")?.Value ??
				token.Claims.FirstOrDefault(c => c.Type == "email")?.Value ??
				"User",
			ExpiresOn = DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds),
			Token = accessToken,
			UserId = token.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? ""
		};
	}

	public async Task LogoutAsync()
	{
		if (_authenticationInfo != null && Config.IsConfigured)
		{
			try
			{
				// Ends the Auth0 browser session too, so the next sign-in can pick a different account.
				var logoutUrl = $"{Authority}/v2/logout?client_id={Uri.EscapeDataString(Config.ClientId!)}";
				await Windows.System.Launcher.LaunchUriAsync(new Uri(logoutUrl));
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error during Auth0 logout: {ex.Message}");
			}
		}

		_authenticationInfo = null;
		_refreshToken = null;
		ClearAuthenticationCache();
	}
}
