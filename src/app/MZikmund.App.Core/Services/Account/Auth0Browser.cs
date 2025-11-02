using IdentityModel.OidcClient.Browser;
using System.Diagnostics;

namespace MZikmund.Services.Account;

/// <summary>
/// Simplified browser implementation for Auth0 OIDC authentication.
/// NOTE: This is a basic implementation that demonstrates the authentication flow.
/// For production use, consider implementing proper callback handling:
/// - Mobile: Use deep linking with proper callback URL schemes
/// - Desktop/WASM: Implement a local HTTP listener to capture callbacks
/// </summary>
public class Auth0Browser : IBrowser
{
	public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = default)
	{
		try
		{
#if __ANDROID__ || __IOS__
			// For mobile platforms, open the system browser with the Auth0 login page
			// NOTE: This is a simplified implementation. In production, you should:
			// 1. Register a custom URL scheme (e.g., "myapp://callback")
			// 2. Handle the callback in your app's deep link handler
			// 3. Extract the authorization code from the callback
			// 4. Return it to the Auth0Client for token exchange
			var uri = new Uri(options.StartUrl);
			await Launcher.Default.OpenAsync(uri);

			// IMPORTANT: This simplified implementation returns immediately.
			// In a real implementation, you would wait for the deep link callback
			// before returning the result. Consider using TaskCompletionSource
			// and completing it when the deep link handler receives the callback.
			return new BrowserResult
			{
				ResultType = BrowserResultType.Success,
				Response = options.EndUrl
			};
#else
			// For desktop/WASM, open in system browser
			// NOTE: This is a simplified implementation. In production, you should:
			// 1. Start a local HTTP listener on a specific port (e.g., http://localhost:8080/callback)
			// 2. Open the Auth0 login page with the callback URL
			// 3. Wait for the HTTP callback with the authorization code
			// 4. Return the callback URL with the code to Auth0Client
			var psi = new ProcessStartInfo
			{
				FileName = options.StartUrl,
				UseShellExecute = true
			};
			Process.Start(psi);

			// IMPORTANT: This delay is a placeholder. In a real implementation,
			// you would wait for the actual HTTP callback before returning.
			await Task.Delay(5000, cancellationToken);

			return new BrowserResult
			{
				ResultType = BrowserResultType.Success,
				Response = options.EndUrl
			};
#endif
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Browser Error: {ex.Message}");
			return new BrowserResult
			{
				ResultType = BrowserResultType.UnknownError,
				Error = ex.Message
			};
		}
	}
}
