using System.Security.Cryptography;
using System.Text;

namespace MZikmund.Shared.Authentication;

/// <summary>
/// Proof Key for Code Exchange (RFC 7636) helpers.
/// </summary>
public static class Pkce
{
	public static string CreateCodeVerifier() => Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

	public static string CreateCodeChallenge(string codeVerifier) =>
		Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)));

	public static string CreateState() => Base64UrlEncode(RandomNumberGenerator.GetBytes(16));

	private static string Base64UrlEncode(byte[] bytes) =>
		Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
