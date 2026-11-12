using System.Security.Claims;
using MZikmund.Web.Core.Services.Comments;
using MZikmund.Web.Infrastructure.Auth;

namespace MZikmund.Web.Services;

/// <summary>
/// Reads the authenticated Supabase identity off the current HTTP request.
/// </summary>
public sealed class CurrentUserAccessor : ICurrentUserAccessor
{
	// Column lengths of CommentAuthor (the display name is capped by DisplayNameRules).
	private const int MaxAvatarUrlLength = 2048;
	private const int MaxEmailLength = 320;
	private const string GoogleAvatarDomain = "googleusercontent.com";

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

			var isAdmin = principal.HasClaim(SupabaseClaims.Role, SupabaseClaims.AdminRole);

			// user_metadata is editable by the user themselves, so the name goes through the same rules as a chosen one.
			// The email is never used as a name.
			var displayName = DisplayNameRules.FromToken(principal.FindFirstValue(SupabaseClaims.DisplayName), id, isAdmin);

			return new CommentAuthorInfo(
				id,
				displayName,
				principal.HasClaim(SupabaseClaims.Provider, SupabaseClaims.GoogleProvider)
					? CleanAvatarUrl(principal.FindFirstValue(SupabaseClaims.AvatarUrl))
					: null,
				email,
				isAdmin);
		}
	}

	// Only absolute https URLs without credentials on Google's avatar host; an over-long URL is dropped rather than cut,
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
			&& IsGoogleAvatarHost(uri.IdnHost)
			? trimmed
			: null;
	}

	private static bool IsGoogleAvatarHost(string host) =>
		host.Equals(GoogleAvatarDomain, StringComparison.OrdinalIgnoreCase)
		|| host.EndsWith("." + GoogleAvatarDomain, StringComparison.OrdinalIgnoreCase);

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
