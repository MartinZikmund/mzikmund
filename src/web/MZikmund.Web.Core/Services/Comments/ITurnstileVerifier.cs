namespace MZikmund.Web.Core.Services.Comments;

/// <summary>
/// Verifies a Cloudflare Turnstile token against the siteverify endpoint.
/// </summary>
public interface ITurnstileVerifier
{
	/// <summary>
	/// Verifies a Turnstile token. Fails closed: a token that cannot be checked did not pass.
	/// </summary>
	/// <param name="token">The client-side token from the CF-Turnstile-Response header.</param>
	/// <param name="remoteIp">The caller's IP address, or null when unknown.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns><c>true</c> only when Cloudflare reports success and the action matches.</returns>
	Task<bool> VerifyAsync(string token, string? remoteIp, CancellationToken cancellationToken);
}
