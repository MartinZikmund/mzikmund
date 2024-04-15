namespace MZikmund.Services.Account;

public static class AuthenticationConstants
{
	public const string ApplicationId = "7e13557a-4799-46b8-9e2b-0f31c41a051e";

	public const string TenantId = "4e973842-1a98-40ec-9542-3c2019f0fb8e";

	public static string[] DefaultScopes { get; } = new string[]
	{
		"api://862d5839-f30f-41a9-ab6f-ff7eef19342c/access_as_user",
		"user.read",
		"profile",
		"offline_access"
	};
}
