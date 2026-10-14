namespace MZikmund.Shared.Authentication;

public sealed record SupabaseSession(string AccessToken, string RefreshToken, int ExpiresIn);
