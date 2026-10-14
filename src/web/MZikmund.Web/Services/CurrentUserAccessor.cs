using System.Security.Claims;
using MZikmund.Web.Core.Services.Comments;
using MZikmund.Web.Infrastructure.Auth;

namespace MZikmund.Web.Services;

/// <summary>
/// Reads the authenticated Supabase identity off the current HTTP request.
/// </summary>
public sealed class CurrentUserAccessor : ICurrentUserAccessor
{
	// Column lengths of CommentAuthor.
	private const int MaxDisplayNameLength = 256;
	private const int MaxAvatarUrlLength = 2048;
	private const int MaxEmailLength = 320;

	private readonly IHttpContextAccessor _httpContextAccessor;

	/// <summary>
	/// Initializes a new instance of the <see cref="CurrentUserAccessor"/> class.
	/// </summary>
	/// <param name="httpContextAccessor">HTTP context accessor.</param>
	public CurrentUserAccessor(IHttpContextAccessor httpContextAccessor) =>
		_httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

	/// <inheritdoc />
	public CommentAuthorInfo? Current
	{
		get
		{
			var principal = _httpContextAccessor.HttpContext?.User;
			if (principal?.Identity is not { IsAuthenticated: true })
			{
				return null;
			}

			if (!Guid.TryParse(principal.FindFirstValue(SupabaseClaims.Subject), out var id))
			{
				return null;
			}

			var email = Truncate(principal.FindFirstValue(SupabaseClaims.Email) ?? "", MaxEmailLength);

			// user_metadata is editable by the user themselves, so nothing in it is trusted as is.
			var displayName = CleanDisplayName(principal.FindFirstValue(SupabaseClaims.DisplayName));
			if (displayName.Length == 0)
			{
				// Apple returns no name in the web OAuth response, and only on first sign-in natively.
				var separatorIndex = email.IndexOf('@');
				displayName = separatorIndex > 0 ? CleanDisplayName(email[..separatorIndex]) : "";
			}

			if (displayName.Length == 0)
			{
				displayName = "Anonymous";
			}

			return new CommentAuthorInfo(
				id,
				displayName,
				CleanAvatarUrl(principal.FindFirstValue(SupabaseClaims.AvatarUrl)),
				email,
				principal.HasClaim(SupabaseClaims.Role, SupabaseClaims.AdminRole));
		}
	}

	private static string CleanDisplayName(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return "";
		}

		var withoutControls = string.Concat(value.Where(c => !char.IsControl(c)));
		return Truncate(withoutControls.Trim(), MaxDisplayNameLength).TrimEnd();
	}

	// Only absolute https URLs without credentials; an over-long URL is dropped rather than cut,
	// because a truncated URL points nowhere.
	private static string? CleanAvatarUrl(string? value)
	{
		var trimmed = value?.Trim();
		if (string.IsNullOrEmpty(trimmed) || trimmed.Length > MaxAvatarUrlLength)
		{
			return null;
		}

		return Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
			&& uri.Scheme == Uri.UriSchemeHttps
			&& string.IsNullOrEmpty(uri.UserInfo)
			? trimmed
			: null;
	}

	private static string Truncate(string value, int maxLength)
	{
		if (value.Length <= maxLength)
		{
			return value;
		}

		// Never cut a surrogate pair in half.
		var length = char.IsHighSurrogate(value[maxLength - 1]) ? maxLength - 1 : maxLength;
		return value[..length];
	}
}
