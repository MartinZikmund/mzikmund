using Microsoft.Identity.Client;
using Uno.UI.MSAL;
using MZikmund.Services.Preferences;
using Microsoft.Identity.Client.Extensions.Msal;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace MZikmund.Services.Account;

public class UserService : IUserService
{
	private IPublicClientApplication? _identityClient;
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

		await EnsureIdentityClientAsync();

		var accounts = await _identityClient.GetAccountsAsync();
		AuthenticationResult? result = null;
		bool tryInteractiveLogin = false;

		try
		{
			result = await _identityClient
				.AcquireTokenSilent(AuthenticationConstants.DefaultScopes, accounts.FirstOrDefault())
				.ExecuteAsync();
		}
		catch (MsalUiRequiredException)
		{
			tryInteractiveLogin = true;
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"MSAL Silent Error: {ex.Message}");
		}

		if (tryInteractiveLogin)
		{
			try
			{
				result = await _identityClient
					.AcquireTokenInteractive(AuthenticationConstants.DefaultScopes)
					.ExecuteAsync();
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"MSAL Interactive Error: {ex.Message}");
			}
		}

		_authenticationInfo = new AuthenticationInfo
		{
			DisplayName = result?.Account?.Username ?? "",
			ExpiresOn = result?.ExpiresOn ?? DateTimeOffset.MinValue,
			Token = result?.AccessToken ?? "",
			UserId = result?.Account?.Username ?? ""
		};
	}

	[MemberNotNull(nameof(_identityClient))]
	private async Task EnsureIdentityClientAsync()
	{
		if (_identityClient == null)
		{
#if __ANDROID__
			_identityClient = PublicClientApplicationBuilder
				.Create(AuthenticationConstants.ApplicationId)
				.WithAuthority(AzureCloudInstance.AzurePublic, AuthenticationConstants.TenantId)
				.WithRedirectUri($"msal{AuthenticationConstants.ApplicationId}://auth")
				.WithParentActivityOrWindow(() => ContextHelper.Current)
				.Build();

			await Task.CompletedTask;
#elif __IOS__
			_identityClient = PublicClientApplicationBuilder
				.Create(AuthenticationConstants.ApplicationId)
				.WithAuthority(AzureCloudInstance.AzurePublic, AuthenticationConstants.TenantId)
				.WithIosKeychainSecurityGroup("com.microsoft.adalcache")
				.WithRedirectUri($"msal{AuthenticationConstants.ApplicationId}://auth")
				.Build();

			await Task.CompletedTask;
#else
			_identityClient = PublicClientApplicationBuilder
				.Create(AuthenticationConstants.ApplicationId)
				.WithAuthority(AzureCloudInstance.AzurePublic, AuthenticationConstants.TenantId)
				.WithRedirectUri("http://localhost")
				.WithUnoHelpers()
				.Build();

			await AttachTokenCacheAsync();
#endif
		}
	}

#if !__ANDROID__ && !__IOS__
	private async Task AttachTokenCacheAsync()
	{
#if !HAS_UNO
		// Cache configuration and hook-up to public application. Refer to https://github.com/AzureAD/microsoft-authentication-extensions-for-dotnet/wiki/Cross-platform-Token-Cache#configuring-the-token-cache
		var storageProperties = new StorageCreationPropertiesBuilder("msal.cache", ApplicationData.Current.LocalFolder.Path)
				.Build();

		var msalcachehelper = await MsalCacheHelper.CreateAsync(storageProperties);
		msalcachehelper.RegisterCache(_identityClient!.UserTokenCache);
#else
		await Task.CompletedTask;
#endif
	}
#endif
}
