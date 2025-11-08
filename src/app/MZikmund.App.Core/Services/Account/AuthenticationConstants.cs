namespace MZikmund.Services.Account;

public static class AuthenticationConstants
{
	public const string Domain = "mzikmunddev.eu.auth0.com";

	public const string Audience = "https://mzikmund.dev/api";

	public static string[] DefaultScopes { get; } = new string[]
	{
		"openid",
		"profile",
		"email",
		"offline_access"
	};
}
