using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.Options;
using MZikmund.Services.Preferences;
using MZikmund.Shared.Authentication;

#if __DESKTOP__
using System.Text;
#endif
#if !WINDOWS && !__DESKTOP__
using Windows.Security.Authentication.Web;
#endif

namespace MZikmund.Services.Account;

public class UserService : IUserService
{
	private static readonly TimeSpan SignInTimeout = TimeSpan.FromMinutes(5);
	private static readonly TimeSpan RefreshMargin = TimeSpan.FromMinutes(5);

	private readonly SemaphoreSlim _authenticationLock = new(1, 1);
	private readonly IAppPreferences _appPreferences;
	private readonly IOptions<SupabaseConfig> _config;

	private SupabaseAuthClient? _client;
	private SupabaseIdentity? _identity;
	private string? _accessToken;
	private string? _refreshToken;

	public UserService(IAppPreferences appPreferences, IOptions<SupabaseConfig> config)
	{
		_appPreferences = appPreferences;
		_config = config;

		LoadCachedSession();
	}

	public event EventHandler? AuthenticationChanged;

	public bool IsLoggedIn => _identity is not null;

	public bool IsAdmin => _identity?.IsAdmin == true;

	public string? UserId => _identity?.UserId;

	public string? UserName => _identity?.DisplayName;

	public string? AvatarUrl => _identity?.AvatarUrl;

	private SupabaseConfig Config => _config.Value;

	private string ExpectedIssuer => $"{Config.Url!.TrimEnd('/')}/auth/v1";

	public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
	{
		// Concurrent API calls share one refresh: GoTrue refresh tokens are single-use.
		await _authenticationLock.WaitAsync(cancellationToken);
		var before = Snapshot();
		try
		{
			if (_identity is null || _accessToken is null)
			{
				throw new AuthenticationFailedException("Sign in to continue.");
			}

			if (_identity.ExpiresOn < DateTimeOffset.UtcNow.Add(RefreshMargin))
			{
				await RefreshCoreAsync(cancellationToken);
			}

			return _accessToken;
		}
		finally
		{
			_authenticationLock.Release();
			RaiseIfChanged(before);
		}
	}

	private async Task RefreshCoreAsync(CancellationToken cancellationToken)
	{
		if (string.IsNullOrEmpty(_refreshToken))
		{
			ClearSession();
			throw new AuthenticationFailedException("Your session expired. Sign in again.");
		}

		try
		{
			SetSession(await GetClient().RefreshAsync(_refreshToken, cancellationToken));
		}
		catch (SupabaseAuthException ex) when (ex.IsRejected)
		{
			Debug.WriteLine($"Token refresh rejected: {ex.Message}");
			ClearSession();
			throw new AuthenticationFailedException("Your session expired. Sign in again.", ex);
		}
		catch (Exception ex) when (ex is HttpRequestException or SupabaseAuthException && _identity!.ExpiresOn > DateTimeOffset.UtcNow)
		{
			// Server or network trouble: keep the session and use the token while it is still valid.
			Debug.WriteLine($"Token refresh failed, using the current token: {ex.Message}");
		}
		catch (SupabaseAuthException ex)
		{
			throw new AuthenticationFailedException(ex.Message, ex);
		}
		catch (FormatException ex)
		{
			throw new AuthenticationFailedException("Sign-in server returned an invalid token.", ex);
		}
	}

	public async Task SignInWithProviderAsync(SupabaseOAuthProvider provider)
	{
		await _authenticationLock.WaitAsync();
		var before = Snapshot();
		try
		{
			EnsureConfigured();

			var codeVerifier = Pkce.CreateCodeVerifier();
			var redirectUri = GetRedirectUri();
			var authUri = SupabaseAuthUris.Authorize(
				Config.Url!,
				provider,
				redirectUri,
				Pkce.CreateCodeChallenge(codeVerifier));

			var callbackUri = await GetAuthorizationCallbackAsync(authUri, redirectUri);
			if (callbackUri is null)
			{
				return;
			}

			// GoTrue keeps its own state with the provider; the PKCE verifier is what binds the code to us.
			var callback = OAuthCallback.Parse(callbackUri, null);
			if (callback.Code is null)
			{
				throw new AuthenticationFailedException($"{callback.Error}: {callback.ErrorDescription}");
			}

			SetSession(await GetClient().ExchangeCodeAsync(callback.Code, codeVerifier));
		}
		catch (Exception ex) when (ex is not AuthenticationFailedException)
		{
			throw new AuthenticationFailedException(ex.Message, ex);
		}
		finally
		{
			_authenticationLock.Release();
			RaiseIfChanged(before);
		}
	}

	public async Task SendEmailCodeAsync(string email)
	{
		EnsureConfigured();

		try
		{
			await GetClient().SendEmailCodeAsync(email.Trim());
		}
		catch (SupabaseAuthException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
		{
			throw new AuthenticationFailedException("Too many codes requested, wait a minute and try again.", ex);
		}
		catch (Exception ex) when (ex is HttpRequestException or SupabaseAuthException)
		{
			throw new AuthenticationFailedException(ex.Message, ex);
		}
	}

	public async Task VerifyEmailCodeAsync(string email, string code)
	{
		EnsureConfigured();

		await _authenticationLock.WaitAsync();
		var before = Snapshot();
		try
		{
			SetSession(await GetClient().VerifyEmailCodeAsync(email.Trim(), code.Trim()));
		}
		catch (SupabaseAuthException ex) when (ex.IsRejected)
		{
			throw new AuthenticationFailedException("That code is wrong or has expired.", ex);
		}
		catch (Exception ex) when (ex is HttpRequestException or SupabaseAuthException or FormatException)
		{
			throw new AuthenticationFailedException(ex.Message, ex);
		}
		finally
		{
			_authenticationLock.Release();
			RaiseIfChanged(before);
		}
	}

	public async Task LogoutAsync()
	{
		await _authenticationLock.WaitAsync();
		var before = Snapshot();
		try
		{
			if (_accessToken is not null && Config.IsConfigured)
			{
				try
				{
					await GetClient().SignOutAsync(_accessToken);
				}
				catch (Exception ex)
				{
					Debug.WriteLine($"Error during sign-out: {ex.Message}");
				}
			}

			ClearSession();
		}
		finally
		{
			_authenticationLock.Release();
			RaiseIfChanged(before);
		}
	}

	private void EnsureConfigured()
	{
		if (!Config.IsConfigured)
		{
			throw new AuthenticationFailedException(
				"Sign-in is not configured. Set SupabaseConfig:Url and SupabaseConfig:PublishableKey " +
				"(MZIKMUND_SupabaseConfig__Url / __PublishableKey). See docs/SUPABASE_AUTH.md.");
		}
	}

	private SupabaseAuthClient GetClient()
	{
		EnsureConfigured();
		return _client ??= new(new HttpClient { Timeout = TimeSpan.FromSeconds(30) }, Config.Url!, Config.PublishableKey!);
	}

	private void LoadCachedSession()
	{
		var accessToken = _appPreferences.AccessToken;
		var refreshToken = _appPreferences.RefreshToken;
		if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken) || !Config.IsConfigured)
		{
			return;
		}

		try
		{
			// A leftover Auth0 session or one from another Supabase project must not be trusted.
			var identity = SupabaseAccessToken.Read(accessToken);
			if (identity.Issuer == ExpectedIssuer)
			{
				_identity = identity;
				_accessToken = accessToken;
				_refreshToken = refreshToken;
				return;
			}
		}
		catch (FormatException)
		{
		}

		ClearAuthenticationCache();
	}

	private void SetSession(SupabaseSession session)
	{
		var identity = SupabaseAccessToken.Read(session.AccessToken);

		_identity = identity;
		_accessToken = session.AccessToken;
		_refreshToken = session.RefreshToken;

		_appPreferences.AccessToken = session.AccessToken;
		_appPreferences.RefreshToken = session.RefreshToken;
		_appPreferences.UserId = identity.UserId;
		_appPreferences.DisplayName = identity.DisplayName;
		_appPreferences.TokenExpiresOn = identity.ExpiresOn;
	}

	private void ClearSession()
	{
		_identity = null;
		_accessToken = null;
		_refreshToken = null;
		ClearAuthenticationCache();
	}

	private void ClearAuthenticationCache()
	{
		_appPreferences.AccessToken = null;
		_appPreferences.RefreshToken = null;
		_appPreferences.UserId = null;
		_appPreferences.DisplayName = null;
		_appPreferences.TokenExpiresOn = null;
	}

	private (bool IsLoggedIn, bool IsAdmin, string? UserId, string? UserName) Snapshot() =>
		(IsLoggedIn, IsAdmin, UserId, UserName);

	private void RaiseIfChanged((bool IsLoggedIn, bool IsAdmin, string? UserId, string? UserName) before)
	{
		if (before != Snapshot())
		{
			AuthenticationChanged?.Invoke(this, EventArgs.Empty);
		}
	}

#if WINDOWS
	private static TaskCompletionSource<Uri>? _pendingCallback;

	/// <summary>
	/// Hands the mzikmund-app://oauthcallback protocol activation to the sign-in that is waiting for it.
	/// </summary>
	public static bool TryCompleteProtocolCallback(Uri uri) =>
		Interlocked.Exchange(ref _pendingCallback, null)?.TrySetResult(uri) == true;

	private static Uri GetRedirectUri() => new("mzikmund-app://oauthcallback");

	private static async Task<Uri?> GetAuthorizationCallbackAsync(Uri authUri, Uri redirectUri)
	{
		TaskCompletionSource<Uri> callback = new(TaskCreationOptions.RunContinuationsAsynchronously);
		Interlocked.Exchange(ref _pendingCallback, callback)?.TrySetCanceled();

		await Windows.System.Launcher.LaunchUriAsync(authUri);

		if (await Task.WhenAny(callback.Task, Task.Delay(SignInTimeout)) != callback.Task)
		{
			Interlocked.CompareExchange(ref _pendingCallback, null, callback);
			throw new AuthenticationFailedException("Sign-in timed out.");
		}

		return await callback.Task;
	}
#elif __DESKTOP__
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
			WebAuthenticationStatus.Success when result.ResponseData is { } data => new Uri(data),
			WebAuthenticationStatus.UserCancel => null,
			_ => throw new AuthenticationFailedException($"Sign-in failed (HTTP {result.ResponseErrorDetail})."),
		};
	}
#endif
}
