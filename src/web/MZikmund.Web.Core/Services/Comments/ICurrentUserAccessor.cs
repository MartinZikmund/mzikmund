namespace MZikmund.Web.Core.Services.Comments;

/// <summary>
/// Exposes the Supabase-authenticated caller to MediatR handlers without coupling them to HttpContext.
/// </summary>
public interface ICurrentUserAccessor
{
	/// <summary>
	/// Gets the current authenticated author, or <c>null</c> when the request is anonymous
	/// or the token carries no usable subject.
	/// </summary>
	CommentAuthorInfo? Current { get; }
}

/// <summary>
/// Identity of the authenticated caller, projected from Supabase access-token claims.
/// </summary>
/// <param name="Id">Supabase user id (the "sub" claim).</param>
/// <param name="DisplayName">Display name, never empty — falls back to the email local part.</param>
/// <param name="AvatarUrl">Avatar URL, or <c>null</c> when the provider supplied none.</param>
/// <param name="Email">Email address from the flat "email" claim.</param>
/// <param name="IsAdmin">Whether app_metadata.role is "admin".</param>
public record CommentAuthorInfo(Guid Id, string DisplayName, string? AvatarUrl, string Email, bool IsAdmin);
