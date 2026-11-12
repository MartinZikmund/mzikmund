namespace MZikmund.Services.Account;

public interface ISignInPrompt
{
	/// <summary>
	/// Returns true when the user is signed in, showing the sign-in dialog first if needed.
	/// </summary>
	Task<bool> EnsureSignedInAsync();
}
