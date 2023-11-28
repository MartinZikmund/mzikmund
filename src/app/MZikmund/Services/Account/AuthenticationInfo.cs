namespace MZikmund.Services.Account;

public class AuthenticationInfo
{
	public required string DisplayName { get; init; }

	public required DateTimeOffset ExpiresOn { get; init; }

	public required string Token { get; init; }

	public required string UserId { get; init; }
}
