namespace MZikmund.Services.Account;

public interface IUserService
{
	bool IsLoggedIn { get; }

	string? AccessToken { get; }

	Task AuthenticateAsync();
}
