using System.Security.Claims;
using MZikmund.Web.Core.Services.Comments;
using MZikmund.Web.Infrastructure.Auth;

namespace MZikmund.Web.Services;

/// <summary>
/// Reads the authenticated Supabase identity off the current HTTP request.
/// </summary>
public sealed class CurrentUserAccessor : ICurrentUserAccessor
{
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

			var email = principal.FindFirstValue(SupabaseClaims.Email) ?? "";
			var displayName = principal.FindFirstValue(SupabaseClaims.DisplayName);
			if (string.IsNullOrWhiteSpace(displayName))
			{
				// Apple returns no name in the web OAuth response, and only on first sign-in natively.
				var separatorIndex = email.IndexOf('@');
				displayName = separatorIndex > 0 ? email[..separatorIndex] : "Anonymous";
			}

			var avatarUrl = principal.FindFirstValue(SupabaseClaims.AvatarUrl);

			return new CommentAuthorInfo(
				id,
				displayName,
				string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl,
				email,
				principal.HasClaim(SupabaseClaims.Role, SupabaseClaims.AdminRole));
		}
	}
}
