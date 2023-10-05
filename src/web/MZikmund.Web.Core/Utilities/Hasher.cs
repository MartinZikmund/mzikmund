using System.Security.Cryptography;
using System.Text;

namespace MZikmund.Web.Core.Utilities;

internal class Hasher
{
	public static string HashPassword(string password)
	{
		if (string.IsNullOrWhiteSpace(password)) return string.Empty;

		var data = Encoding.UTF8.GetBytes(password);
		using var sha = SHA256.Create();
		sha.TransformFinalBlock(data, 0, data.Length);
		return Convert.ToBase64String(sha.Hash ?? throw new InvalidOperationException());
	}
}
