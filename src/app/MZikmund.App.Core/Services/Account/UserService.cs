using Auth0.OidcClient;
using IdentityModel.OidcClient;
using IdentityModel.OidcClient.Browser;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace MZikmund.Services.Account;

public class UserService : IUserService
{
	private Auth0Client? _auth0Client;
	private AuthenticationInfo? _authenticationInfo;
	private readonly IBrowser _browser;

	public UserService(IBrowser browser)
	{
		_browser = browser;
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

		EnsureAuth0Client();

		LoginResult? result = null;

		try
		{
			result = await _auth0Client.LoginAsync();

			if (result.IsError)
			{
				Debug.WriteLine($"Auth0 Error: {result.Error}");
				return;
			}

			_authenticationInfo = new AuthenticationInfo
			{
				DisplayName = result.User?.Identity?.Name ?? "",
				ExpiresOn = result.AccessTokenExpiration,
				Token = result.AccessToken,
				UserId = result.User?.FindFirst("sub")?.Value ?? ""
			};
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Auth0 Error: {ex.Message}");
		}
	}

	[MemberNotNull(nameof(_auth0Client))]
	private void EnsureAuth0Client()
	{
		if (_auth0Client == null)
		{
			var options = new Auth0ClientOptions
			{
				Domain = AuthenticationConstants.Domain,
				ClientId = AuthenticationConstants.ClientId,
				Scope = string.Join(" ", AuthenticationConstants.DefaultScopes),
				Browser = _browser
			};

			// Add audience if needed for API calls
			if (!string.IsNullOrEmpty(AuthenticationConstants.Audience))
			{
				options.Audience = AuthenticationConstants.Audience;
			}

			_auth0Client = new Auth0Client(options);
		}
	}
}
