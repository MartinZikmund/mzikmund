using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using MZikmund.App.Core.Infrastructure;
using MZikmund.Services.Preferences;
using Microsoft.UI;
using MZikmund.Business.Models;
using Microsoft.Extensions.Options;

#if WINDOWS
using Microsoft.Security.Authentication.OAuth;
#endif

namespace MZikmund.Services.Account;

public class UserService : IUserService
{
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

		// Load cached authentication info on initialization
		LoadCachedAuthentication();
	}

	public bool IsLoggedIn => _authenticationInfo != null;

	public string? UserName => _authenticationInfo?.DisplayName;

	public bool NeedsRefresh => _authenticationInfo?.ExpiresOn < DateTimeOffset.UtcNow.AddMinutes(-5);

	public string? AccessToken => _authenticationInfo?.Token;

	private void LoadCachedAuthentication()
	{
		// Try to restore authentication from cache
		var accessToken = _appPreferences.AccessToken;
		var refreshToken = _appPreferences.RefreshToken;
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

			_refreshToken = refreshToken;

			Debug.WriteLine($"Loaded cached authentication for user: {displayName}");
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

			Debug.WriteLine($"Saved authentication to cache for user: {_authenticationInfo.DisplayName}");
		}
	}

	private void ClearAuthenticationCache()
	{
		_appPreferences.AccessToken = null;
		_appPreferences.RefreshToken = null;
		_appPreferences.UserId = null;
		_appPreferences.DisplayName = null;
		_appPreferences.TokenExpiresOn = null;

		Debug.WriteLine("Cleared authentication cache");
	}

	public async Task AuthenticateAsync()
	{
		if (IsLoggedIn && !NeedsRefresh)
		{
			return;
		}

		// Validate that Auth0 configuration is not using placeholder values
		if (string.IsNullOrEmpty(_authConfig.Value.ClientId) ||
			_authConfig.Value.ClientId.Contains("your-auth0-client-id"))
		{
			throw new InvalidOperationException(
				"Auth0 configuration is not set or contains placeholder values. " +
				"Please update AuthenticationConstants.cs with your actual Auth0 configuration. " +
				"See docs/AUTH0_MIGRATION.md for details.");
		}

		try
		{
			// Try refresh token first if we have one
			if (!string.IsNullOrEmpty(_refreshToken))
			{
				var refreshed = await RefreshTokenAsync();
				if (refreshed)
				{
					SaveAuthenticationToCache();
					return;
				}
			}

			// Perform new authentication
			await PerformAuthenticationAsync();
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Auth0 Error: {ex.Message}");
			// Clear cache on authentication failure
			ClearAuthenticationCache();
		}
	}

	private async Task PerformAuthenticationAsync()
	{
#if WINDOWS
		if (_application.MainWindow is null)
		{
			throw new InvalidOperationException("Window is not available");
		}

		// Use OAuth2Manager on Windows platforms
		var authority = $"https://{AuthenticationConstants.Domain}";

		// Build authorization URL
		var authUrl = $"{authority}/authorize";

		// Build scope string
		var scopes = string.Join(" ", AuthenticationConstants.DefaultScopes);

		// Create authorization request parameters for Auth0
		// OAuth2Manager handles PKCE automatically, so we don't need to generate code verifier/challenge
		var authRequestParams = AuthRequestParams.CreateForAuthorizationCodeRequest(
			_authConfig.Value.ClientId,
			new Uri("mzikmund-app://oauthcallback"));

		authRequestParams.Scope = scopes;

		// Auth0 requires audience parameter
		if (!string.IsNullOrEmpty(AuthenticationConstants.Audience) &&
			!AuthenticationConstants.Audience.Contains("your-api-identifier"))
		{
			// Create full auth URL with audience parameter
			authUrl = $"{authUrl}?audience={Uri.EscapeDataString(AuthenticationConstants.Audience)}";
		}

		// Get window ID
		var windowId = GetWindowHandle();

		// Perform OAuth2 authorization
		var authResult = await OAuth2Manager.RequestAuthWithParamsAsync(
			windowId,
			new Uri(authUrl),
			authRequestParams);

		if (authResult.Response != null)
		{
			// Use OAuth2Manager to exchange the authorization code for tokens
			var tokenRequestParams = TokenRequestParams.CreateForAuthorizationCodeRequest(authResult.Response);

			var tokenEndpoint = $"{authority}/oauth/token";
			var tokenResult = await OAuth2Manager.RequestTokenAsync(
				new Uri(tokenEndpoint),
				tokenRequestParams);

			if (tokenResult.Response != null)
			{
				var tokenResponse = tokenResult.Response;
				_refreshToken = tokenResponse.RefreshToken;

				// Parse JWT to get user info
				var handler = new JwtSecurityTokenHandler();
				var token = handler.ReadJwtToken(tokenResponse.AccessToken);

				var expiresOn = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

				_authenticationInfo = new AuthenticationInfo
				{
					DisplayName = token.Claims.FirstOrDefault(c => c.Type == "name")?.Value ??
								  token.Claims.FirstOrDefault(c => c.Type == "email")?.Value ??
								  "User",
					ExpiresOn = expiresOn,
					Token = tokenResponse.AccessToken,
					UserId = token.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? ""
				};

				// Save to cache after successful authentication
				SaveAuthenticationToCache();
			}
			else if (tokenResult.Failure != null)
			{
				Debug.WriteLine($"Token exchange failed: {tokenResult.Failure.Error} - {tokenResult.Failure.ErrorDescription}");
			}
		}
		else if (authResult.Failure != null)
		{
			if (authResult.Failure.Error == "user_cancelled")
			{
				Debug.WriteLine("User cancelled authentication");
			}
			else
			{
				Debug.WriteLine($"Authentication error: {authResult.Failure.Error} - {authResult.Failure.ErrorDescription}");
			}
		}
#else
		//// For non-Windows platforms, use WebAuthenticator from Uno Platform
		//var authority = $"https://{AuthenticationConstants.Domain}";
		//var redirectUri = "dev.mzikmund.MZikmund.App://callback";

		//// Generate PKCE code verifier and challenge
		//var codeVerifier = GenerateCodeVerifier();
		//var codeChallenge = GenerateCodeChallenge(codeVerifier);

		//// Build authorization URL
		//var authUrl = $"{authority}/authorize?" +
		//	$"client_id={Uri.EscapeDataString(AuthenticationConstants.ClientId)}&" +
		//	$"response_type=code&" +
		//	$"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
		//	$"scope={Uri.EscapeDataString(string.Join(" ", AuthenticationConstants.DefaultScopes))}&" +
		//	$"code_challenge={Uri.EscapeDataString(codeChallenge)}&" +
		//	$"code_challenge_method=S256";

		//if (!string.IsNullOrEmpty(AuthenticationConstants.Audience) &&
		//	!AuthenticationConstants.Audience.Contains("your-api-identifier"))
		//{
		//	authUrl += $"&audience={Uri.EscapeDataString(AuthenticationConstants.Audience)}";
		//}

		//// Use platform WebAuthenticator
		//var result = await WebAuthenticator.Default.AuthenticateAsync(
		//	new Uri(authUrl),
		//	new Uri(redirectUri));

		//if (result?.Properties?.TryGetValue("code", out var code) == true && !string.IsNullOrEmpty(code))
		//{
		//	await ExchangeCodeForTokensAsync(code, codeVerifier, redirectUri);
		//}
#endif
	}

	private async Task<bool> RefreshTokenAsync()
	{
		if (string.IsNullOrEmpty(_refreshToken))
		{
			return false;
		}

#if WINDOWS
		// Use OAuth2Manager for token refresh
		var authority = $"https://{AuthenticationConstants.Domain}";
		var tokenEndpoint = $"{authority}/oauth/token";

		var tokenRequestParams = TokenRequestParams.CreateForRefreshToken(_refreshToken);

		try
		{
			var tokenResult = await OAuth2Manager.RequestTokenAsync(
				new Uri(tokenEndpoint),
				tokenRequestParams);

			if (tokenResult.Response != null)
			{
				var tokenResponse = tokenResult.Response;

				// Update refresh token if a new one is provided
				if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
				{
					_refreshToken = tokenResponse.RefreshToken;
				}

				// Parse JWT to get user info
				var handler = new JwtSecurityTokenHandler();
				var token = handler.ReadJwtToken(tokenResponse.AccessToken);

				var expiresOn = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

				_authenticationInfo = new AuthenticationInfo
				{
					DisplayName = token.Claims.FirstOrDefault(c => c.Type == "name")?.Value ??
								  token.Claims.FirstOrDefault(c => c.Type == "email")?.Value ??
								  "User",
					ExpiresOn = expiresOn,
					Token = tokenResponse.AccessToken,
					UserId = token.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? ""
				};

				return true;
			}
			else if (tokenResult.Failure != null)
			{
				Debug.WriteLine($"Token refresh failed: {tokenResult.Failure.Error} - {tokenResult.Failure.ErrorDescription}");
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Token refresh failed: {ex.Message}");
		}
#else
		var authority = $"https://{AuthenticationConstants.Domain}";
		var tokenEndpoint = $"{authority}/oauth/token";

		var requestData = new Dictionary<string, string>
		{
			{ "grant_type", "refresh_token" },
			{ "client_id", _authConfig.Value.ClientId },
			{ "refresh_token", _refreshToken }
		};

		using var httpClient = new HttpClient();
		using var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
		{
			Content = new FormUrlEncodedContent(requestData)
		};

		try
		{
			var response = await httpClient.SendAsync(request);
			var responseContent = await response.Content.ReadAsStringAsync();

			if (response.IsSuccessStatusCode)
			{
				var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent);
				if (tokenResponse != null)
				{
					if (!string.IsNullOrEmpty(tokenResponse.refresh_token))
					{
						_refreshToken = tokenResponse.refresh_token;
					}

					// Parse JWT to get user info
					var handler = new JwtSecurityTokenHandler();
					var token = handler.ReadJwtToken(tokenResponse.access_token);

					var expiresOn = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.expires_in);

					_authenticationInfo = new AuthenticationInfo
					{
						DisplayName = token.Claims.FirstOrDefault(c => c.Type == "name")?.Value ??
									  token.Claims.FirstOrDefault(c => c.Type == "email")?.Value ??
									  "User",
						ExpiresOn = expiresOn,
						Token = tokenResponse.access_token,
						UserId = token.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? ""
					};

					return true;
				}
			}
		}
		catch (HttpRequestException ex)
		{
			Debug.WriteLine($"Token refresh failed (HTTP error): {ex.Message}");
		}
		catch (TaskCanceledException ex)
		{
			Debug.WriteLine($"Token refresh failed (request timed out): {ex.Message}");
		}
		catch (JsonException ex)
		{
			Debug.WriteLine($"Token refresh failed (JSON error): {ex.Message}");
		}
#endif

		return false;
	}

#if WINDOWS
	private WindowId GetWindowHandle()
	{
		var window = _application.MainWindow;
		var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(window);
		return new WindowId((ulong)windowHandle);
	}
#endif

	public async Task LogoutAsync()
	{
#if WINDOWS
		// For Auth0, we should logout by opening the browser to the logout endpoint
		// This ensures the Auth0 session is properly terminated
		if (_application.MainWindow != null && _authenticationInfo != null)
		{
			try
			{
				var authority = $"https://{AuthenticationConstants.Domain}";
				var returnTo = Uri.EscapeDataString("mzikmund-app://logout");

				// Build Auth0 logout URL
				// See: https://auth0.com/docs/api/authentication#logout
				var logoutUrl = $"{authority}/v2/logout?" +
					$"client_id={Uri.EscapeDataString(_authConfig.Value.ClientId)}&" +
					$"returnTo={returnTo}";

				// Open the logout URL in the browser
				// This will clear the Auth0 session cookies
				await Windows.System.Launcher.LaunchUriAsync(new Uri(logoutUrl));

				Debug.WriteLine("Auth0 logout initiated in browser");
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error during Auth0 logout: {ex.Message}");
			}
		}
#endif

		// Clear local authentication state
		_authenticationInfo = null;
		_refreshToken = null;
		ClearAuthenticationCache();
		Debug.WriteLine("User logged out locally");
	}

#if !WINDOWS
	private sealed class TokenResponse
	{
		public string access_token { get; set; } = "";
		public string? refresh_token { get; set; }
		public int expires_in { get; set; }
		public string token_type { get; set; } = "";
	}
#endif
}
