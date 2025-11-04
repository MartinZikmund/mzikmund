using Auth0.OidcClient;
using Duende.IdentityModel.OidcClient;
using Duende.IdentityModel.OidcClient.Browser;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace MZikmund.Services.Account;

public class UserService : IUserService
{
	private Auth0Client? _auth0Client;
	private AuthenticationInfo? _authenticationInfo;

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

			var options = new Auth0ClientOptions
			{
				Domain = AuthenticationConstants.Domain,
				ClientId = AuthenticationConstants.ClientId,
				Scope = string.Join(" ", AuthenticationConstants.DefaultScopes),
				Browser = new WebAuthenticatorBrowser()
			};

			_auth0Client = new Auth0Client(options);
		}
	}
}
