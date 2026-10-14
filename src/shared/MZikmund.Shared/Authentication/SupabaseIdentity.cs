namespace MZikmund.Shared.Authentication;

public sealed record SupabaseIdentity(
	string UserId,
	string? Email,
	string DisplayName,
	string? AvatarUrl,
	bool IsAdmin,
	string Issuer,
	DateTimeOffset ExpiresOn);
