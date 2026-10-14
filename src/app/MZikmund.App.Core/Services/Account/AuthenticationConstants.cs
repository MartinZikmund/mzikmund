namespace MZikmund.Services.Account;

public static class AuthenticationConstants
{
	public static string[] DefaultScopes { get; } = new string[]
	{
		"openid",
		"profile",
		"email",
		"offline_access"
	};
}
