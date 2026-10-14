using System.Text.Json;

namespace MZikmund.Shared.Authentication;

public static class SupabaseAccessToken
{
	/// <summary>
	/// Reads the claims of a Supabase access token. The signature is not checked: the API validates
	/// tokens, the app only reads its own.
	/// </summary>
	public static SupabaseIdentity Read(string accessToken)
	{
		try
		{
			var parts = accessToken.Split('.');
			if (parts.Length != 3)
			{
				throw new FormatException("The access token is not a JWT.");
			}

			using var document = JsonDocument.Parse(Base64UrlDecode(parts[1]));
			var root = document.RootElement;

			var email = GetString(root, "email");
			var userMetadata = GetObject(root, "user_metadata");
			var appMetadata = GetObject(root, "app_metadata");

			var displayName = GetString(userMetadata, "full_name") ??
				GetString(userMetadata, "name") ??
				email ??
				"User";

			return new SupabaseIdentity(
				GetString(root, "sub") ?? throw new FormatException("The access token has no subject."),
				email,
				displayName,
				GetString(userMetadata, "avatar_url") ?? GetString(userMetadata, "picture"),
				GetString(appMetadata, "role") == "admin",
				GetString(root, "iss") ?? "",
				DateTimeOffset.FromUnixTimeSeconds(root.GetProperty("exp").GetInt64()));
		}
		catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException or ArgumentException)
		{
			throw new FormatException("The access token is malformed.", ex);
		}
	}

	private static JsonElement? GetObject(JsonElement? parent, string name) =>
		parent is { ValueKind: JsonValueKind.Object } element &&
		element.TryGetProperty(name, out var value) &&
		value.ValueKind == JsonValueKind.Object ?
			value :
			null;

	private static string? GetString(JsonElement? parent, string name) =>
		parent is { ValueKind: JsonValueKind.Object } element &&
		element.TryGetProperty(name, out var value) &&
		value.ValueKind == JsonValueKind.String ?
			value.GetString() :
			null;

	private static byte[] Base64UrlDecode(string value)
	{
		var padded = value.Replace('-', '+').Replace('_', '/');
		padded = padded.PadRight(padded.Length + ((4 - (padded.Length % 4)) % 4), '=');
		return Convert.FromBase64String(padded);
	}
}
