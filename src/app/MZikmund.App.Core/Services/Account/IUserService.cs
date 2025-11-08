namespace MZikmund.Services.Account;

public interface IUserService
{
	bool IsLoggedIn { get; }

	bool NeedsRefresh { get; }

	string? UserName { get; }

	string? AccessToken { get; }

	Task AuthenticateAsync();

	Task LogoutAsync();
}
