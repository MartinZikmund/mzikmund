using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using MZikmund.DataContracts.Blog.Comments;

namespace MZikmund.Web.Core.Services.Comments;

/// <summary>
/// Why a display name was rejected.
/// </summary>
public enum DisplayNameProblem
{
	None,
	TooShort,
	TooLong,
	NoLettersOrDigits,
	Reserved,
}

/// <summary>
/// Outcome of <see cref="DisplayNameRules.Check"/>.
/// </summary>
/// <param name="Name">The normalized name (set even when it is rejected).</param>
/// <param name="Problem">What is wrong with it, or <see cref="DisplayNameProblem.None"/>.</param>
public readonly record struct DisplayNameCheck(string Name, DisplayNameProblem Problem)
{
	public bool IsValid => Problem == DisplayNameProblem.None;

	public bool IsReserved => Problem == DisplayNameProblem.Reserved;

	public bool IsInvalid => Problem is not (DisplayNameProblem.None or DisplayNameProblem.Reserved);
}

/// <summary>
/// Normalization, validation and fallback rules for comment display names.
/// </summary>
public static class DisplayNameRules
{
	public const int MinLength = CommentLimits.MinDisplayNameLength;
	public const int MaxLength = CommentLimits.MaxDisplayNameLength;

	private static readonly string[] ReservedFragments = ["martinzikmund", "mzikmund"];

	private static readonly HashSet<string> ReservedNames = new(StringComparer.Ordinal)
	{
		"admin", "administrator", "moderator", "mod", "author", "owner", "system", "anonymous", "deleted", "support",
	};

	/// <summary>
	/// NFC, without control and format characters (zero-width, bidi), whitespace runs collapsed, trimmed.
	/// </summary>
	public static string Normalize(string? value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return "";
		}

		StringBuilder builder = new(value.Length);
		var pendingSpace = false;
		foreach (var c in value.Normalize(NormalizationForm.FormC))
		{
			if (char.IsWhiteSpace(c))
			{
				pendingSpace = builder.Length > 0;
				continue;
			}

			var category = char.GetUnicodeCategory(c);
			if (category is UnicodeCategory.Control or UnicodeCategory.Format)
			{
				continue;
			}

			if (pendingSpace)
			{
				builder.Append(' ');
				pendingSpace = false;
			}

			builder.Append(c);
		}

		return builder.ToString();
	}

	/// <summary>
	/// Normalizes and validates a name. Reserved names are accepted when <paramref name="isAdmin"/> is set.
	/// </summary>
	public static DisplayNameCheck Check(string? value, bool isAdmin)
	{
		var name = Normalize(value);
		var length = new StringInfo(name).LengthInTextElements;

		if (length < MinLength)
		{
			return new(name, DisplayNameProblem.TooShort);
		}

		if (length > MaxLength)
		{
			return new(name, DisplayNameProblem.TooLong);
		}

		if (!name.Any(char.IsLetterOrDigit))
		{
			return new(name, DisplayNameProblem.NoLettersOrDigits);
		}

		if (!isAdmin && IsReserved(name))
		{
			return new(name, DisplayNameProblem.Reserved);
		}

		return new(name, DisplayNameProblem.None);
	}

	public static bool IsReserved(string name)
	{
		var key = new string(name.Normalize(NormalizationForm.FormKC).ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
		return ReservedNames.Contains(key) || ReservedFragments.Any(key.Contains);
	}

	/// <summary>
	/// Stable per-user placeholder, e.g. <c>Reader 3FA9</c>.
	/// </summary>
	public static string FallbackName(Guid userId)
	{
		var hash = SHA256.HashData(Encoding.UTF8.GetBytes(userId.ToString()));
		return "Reader " + Convert.ToHexString(hash, 0, 2);
	}

	/// <summary>
	/// The name to use for a token-supplied name: itself when it passes the rules, otherwise the fallback.
	/// </summary>
	public static string FromToken(string? tokenName, Guid userId, bool isAdmin)
	{
		var check = Check(tokenName, isAdmin);
		return check.IsValid ? check.Name : FallbackName(userId);
	}
}
