namespace MZikmund.Services.Account;

public static class AuthenticationConstants
{
	// TODO: Replace with actual Auth0 domain from your Auth0 tenant
	// Example: "your-tenant.auth0.com" or "your-tenant.us.auth0.com"
	public const string Domain = "your-auth0-domain.auth0.com";

	// TODO: Replace with your Auth0 Native Application Client ID
	// Found in Auth0 Dashboard > Applications > Your Native App > Settings
	public const string ClientId = "your-auth0-client-id";

	// TODO: Replace with your Auth0 API Identifier
	// Found in Auth0 Dashboard > Applications > APIs > Your API > Settings
	// Example: "https://api.mzikmund.dev"
	public const string Audience = "your-api-identifier";

	public static string[] DefaultScopes { get; } = new string[]
	{
		"openid",
		"profile",
		"email",
		"offline_access"
	};
}
