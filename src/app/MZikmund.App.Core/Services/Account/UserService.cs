using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
#if WINDOWS
using Windows.Security.Authentication.Web;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
#endif

namespace MZikmund.Services.Account;

public class UserService : IUserService
{
	private AuthenticationInfo? _authenticationInfo;
	private string? _refreshToken;

	public UserService()
	{
	}

	public bool IsLoggedIn => _authenticationInfo != null;

	public string? UserName => _authenticationInfo?.DisplayName;

	public bool NeedsRefresh => _authenticationInfo?.ExpiresOn < DateTimeOffset.UtcNow.AddMinutes(-5);

	public string? AccessToken => _authenticationInfo?.Token;

	public async Task AuthenticateAsync()
	{
		if (IsLoggedIn && !NeedsRefresh)
		{
			return;
		}

		// Validate that Auth0 configuration is not using placeholder values
		if (string.IsNullOrEmpty(AuthenticationConstants.Domain) ||
			AuthenticationConstants.Domain.Contains("your-auth0-domain") ||
			string.IsNullOrEmpty(AuthenticationConstants.ClientId) ||
			AuthenticationConstants.ClientId.Contains("your-auth0-client-id"))
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
					return;
				}
			}

			// Perform new authentication
			await PerformAuthenticationAsync();
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Auth0 Error: {ex.Message}");
		}
	}

	private async Task PerformAuthenticationAsync()
	{
#if WINDOWS
		// Use OAuth2Manager on Windows platforms
		var authority = $"https://{AuthenticationConstants.Domain}";
		var redirectUri = "https://localhost/callback";
		
		// Generate PKCE code verifier and challenge
		var codeVerifier = GenerateCodeVerifier();
		var codeChallenge = GenerateCodeChallenge(codeVerifier);

		// Build authorization URL
		var authUrl = $"{authority}/authorize?" +
			$"client_id={Uri.EscapeDataString(AuthenticationConstants.ClientId)}&" +
			$"response_type=code&" +
			$"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
			$"scope={Uri.EscapeDataString(string.Join(" ", AuthenticationConstants.DefaultScopes))}&" +
			$"code_challenge={Uri.EscapeDataString(codeChallenge)}&" +
			$"code_challenge_method=S256";

		if (!string.IsNullOrEmpty(AuthenticationConstants.Audience) && 
			!AuthenticationConstants.Audience.Contains("your-api-identifier"))
		{
			authUrl += $"&audience={Uri.EscapeDataString(AuthenticationConstants.Audience)}";
		}

		var startUri = new Uri(authUrl);
		var endUri = new Uri(redirectUri);

		// Use WebAuthenticationBroker for OAuth2 authentication
		var result = await WebAuthenticationBroker.AuthenticateAsync(
			WebAuthenticationOptions.None,
			startUri,
			endUri);

		if (result.ResponseStatus == WebAuthenticationStatus.Success)
		{
			// Extract authorization code from response
			var responseUri = new Uri(result.ResponseData);
			var code = ParseQueryString(responseUri.Query, "code");

			if (!string.IsNullOrEmpty(code))
			{
				// Exchange code for tokens
				await ExchangeCodeForTokensAsync(code, codeVerifier, redirectUri);
			}
		}
		else if (result.ResponseStatus == WebAuthenticationStatus.UserCancel)
		{
			Debug.WriteLine("User cancelled authentication");
		}
		else
		{
			Debug.WriteLine($"Authentication error: {result.ResponseErrorDetail}");
		}
#else
		// For non-Windows platforms, use WebAuthenticator from Uno Platform
		var authority = $"https://{AuthenticationConstants.Domain}";
		var redirectUri = "dev.mzikmund.MZikmund.App://callback";
		
		// Generate PKCE code verifier and challenge
		var codeVerifier = GenerateCodeVerifier();
		var codeChallenge = GenerateCodeChallenge(codeVerifier);

		// Build authorization URL
		var authUrl = $"{authority}/authorize?" +
			$"client_id={Uri.EscapeDataString(AuthenticationConstants.ClientId)}&" +
			$"response_type=code&" +
			$"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
			$"scope={Uri.EscapeDataString(string.Join(" ", AuthenticationConstants.DefaultScopes))}&" +
			$"code_challenge={Uri.EscapeDataString(codeChallenge)}&" +
			$"code_challenge_method=S256";

		if (!string.IsNullOrEmpty(AuthenticationConstants.Audience) && 
			!AuthenticationConstants.Audience.Contains("your-api-identifier"))
		{
			authUrl += $"&audience={Uri.EscapeDataString(AuthenticationConstants.Audience)}";
		}

		// Use platform WebAuthenticator
		var result = await WebAuthenticator.Default.AuthenticateAsync(
			new Uri(authUrl),
			new Uri(redirectUri));

		if (result?.Properties?.TryGetValue("code", out var code) == true && !string.IsNullOrEmpty(code))
		{
			await ExchangeCodeForTokensAsync(code, codeVerifier, redirectUri);
		}
#endif
	}

	private async Task ExchangeCodeForTokensAsync(string code, string codeVerifier, string redirectUri)
	{
		var authority = $"https://{AuthenticationConstants.Domain}";
		var tokenEndpoint = $"{authority}/oauth/token";

		var requestData = new Dictionary<string, string>
		{
			{ "grant_type", "authorization_code" },
			{ "client_id", AuthenticationConstants.ClientId },
			{ "code", code },
			{ "redirect_uri", redirectUri },
			{ "code_verifier", codeVerifier }
		};

		using var httpClient = new HttpClient();
		using var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
		{
			Content = new FormUrlEncodedContent(requestData)
		};

		var response = await httpClient.SendAsync(request);
		var responseContent = await response.Content.ReadAsStringAsync();

		if (response.IsSuccessStatusCode)
		{
			var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent);
			if (tokenResponse != null)
			{
				_refreshToken = tokenResponse.refresh_token;

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
			}
		}
		else
		{
			Debug.WriteLine($"Token exchange failed: {responseContent}");
		}
	}

	private async Task<bool> RefreshTokenAsync()
	{
		if (string.IsNullOrEmpty(_refreshToken))
		{
			return false;
		}

		var authority = $"https://{AuthenticationConstants.Domain}";
		var tokenEndpoint = $"{authority}/oauth/token";

		var requestData = new Dictionary<string, string>
		{
			{ "grant_type", "refresh_token" },
			{ "client_id", AuthenticationConstants.ClientId },
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
		catch (Exception ex)
		{
			Debug.WriteLine($"Token refresh failed: {ex.Message}");
		}

		return false;
	}

	private string GenerateCodeVerifier()
	{
		const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~";
		var random = new Random();
		var verifier = new char[128];
		
		for (int i = 0; i < verifier.Length; i++)
		{
			verifier[i] = chars[random.Next(chars.Length)];
		}
		
		return new string(verifier);
	}

	private string GenerateCodeChallenge(string codeVerifier)
	{
#if WINDOWS
		// Use Windows Cryptography API
		var buffer = CryptographicBuffer.ConvertStringToBinary(codeVerifier, BinaryStringEncoding.Utf8);
		var hashAlgorithm = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha256);
		var hash = hashAlgorithm.HashData(buffer);
		var challenge = CryptographicBuffer.EncodeToBase64String(hash);
		
		// Convert to URL-safe base64
		return challenge.TrimEnd('=').Replace('+', '-').Replace('/', '_');
#else
		// Use standard .NET cryptography
		using var sha256 = System.Security.Cryptography.SHA256.Create();
		var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
		var challenge = Convert.ToBase64String(hash);
		
		// Convert to URL-safe base64
		return challenge.TrimEnd('=').Replace('+', '-').Replace('/', '_');
#endif
	}

	private string? ParseQueryString(string query, string key)
	{
		if (string.IsNullOrEmpty(query))
		{
			return null;
		}

		// Remove leading '?' if present
		if (query.StartsWith("?"))
		{
			query = query.Substring(1);
		}

		var pairs = query.Split('&');
		foreach (var pair in pairs)
		{
			var parts = pair.Split('=');
			if (parts.Length == 2 && parts[0] == key)
			{
				return Uri.UnescapeDataString(parts[1]);
			}
		}

		return null;
	}

	private class TokenResponse
	{
		public string access_token { get; set; } = "";
		public string? refresh_token { get; set; }
		public int expires_in { get; set; }
		public string token_type { get; set; } = "";
	}
}
