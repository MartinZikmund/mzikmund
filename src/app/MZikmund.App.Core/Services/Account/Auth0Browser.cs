using IdentityModel.OidcClient.Browser;
using System.Diagnostics;

namespace MZikmund.Services.Account;

public class Auth0Browser : IBrowser
{
	public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = default)
	{
		try
		{
#if __ANDROID__ || __IOS__
			// For mobile platforms, use the system browser
			var uri = new Uri(options.StartUrl);
			await Launcher.Default.OpenAsync(uri);

			// Wait for redirect - this is simplified, you may need a better implementation
			// In production, you'd typically use a deep link handler
			return new BrowserResult
			{
				ResultType = BrowserResultType.Success,
				Response = options.EndUrl
			};
#else
			// For desktop/WASM, open in system browser
			var psi = new ProcessStartInfo
			{
				FileName = options.StartUrl,
				UseShellExecute = true
			};
			Process.Start(psi);

			// For desktop, you'd typically implement a local callback listener
			// This is a simplified implementation
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
