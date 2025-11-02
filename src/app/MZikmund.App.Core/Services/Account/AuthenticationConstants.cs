namespace MZikmund.Services.Account;

public static class AuthenticationConstants
{
	public const string Domain = "your-auth0-domain.auth0.com";

	public const string ClientId = "your-auth0-client-id";

	public const string Audience = "your-api-identifier";

	public static string[] DefaultScopes { get; } = new string[]
	{
		"openid",
		"profile",
		"email",
		"offline_access"
	};
}
