using MZikmund.Shared.Authentication;

namespace MZikmund.Services.Account;

public interface IUserService
{
	bool IsLoggedIn { get; }

	bool IsAdmin { get; }

	string? UserId { get; }

	string? UserName { get; }

	string? AvatarUrl { get; }

	event EventHandler? AuthenticationChanged;

	/// <summary>
	/// Returns a fresh access token, refreshing it first when it expires within five minutes.
	/// Throws AuthenticationFailedException when signed out or when the refresh is rejected.
	/// </summary>
	Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Signs in through the browser. Cancelling the token abandons the wait and returns without a session.
	/// </summary>
	Task SignInWithProviderAsync(SupabaseOAuthProvider provider, CancellationToken cancellationToken = default);

	Task SendEmailCodeAsync(string email);

	Task VerifyEmailCodeAsync(string email, string code);

	Task LogoutAsync();
}
