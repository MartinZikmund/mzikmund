namespace MZikmund.Shared.Authentication;

public enum SupabaseOAuthProvider
{
	Google,
	Apple,
}

public static class SupabaseAuthUris
{
	public static Uri Authorize(string baseUrl, SupabaseOAuthProvider provider, Uri redirectUri, string codeChallenge) =>
		new(
			$"{Endpoint(baseUrl, "authorize").AbsoluteUri}" +
			$"?provider={Uri.EscapeDataString(provider.ToString().ToLowerInvariant())}" +
			$"&redirect_to={Uri.EscapeDataString(redirectUri.AbsoluteUri)}" +
			$"&code_challenge={Uri.EscapeDataString(codeChallenge)}" +
			"&code_challenge_method=s256");

	public static Uri Endpoint(string baseUrl, string relative) =>
		new($"{baseUrl.TrimEnd('/')}/auth/v1/{relative}");
}
