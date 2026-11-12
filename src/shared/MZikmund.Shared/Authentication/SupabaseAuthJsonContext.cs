using System.Text.Json.Serialization;

namespace MZikmund.Shared.Authentication;

internal sealed record PkceExchangeRequest(
	[property: JsonPropertyName("auth_code")] string AuthCode,
	[property: JsonPropertyName("code_verifier")] string CodeVerifier);

internal sealed record RefreshRequest(
	[property: JsonPropertyName("refresh_token")] string RefreshToken);

internal sealed record SendOtpRequest(
	[property: JsonPropertyName("email")] string Email,
	[property: JsonPropertyName("create_user")] bool CreateUser);

internal sealed record VerifyOtpRequest(
	[property: JsonPropertyName("type")] string Type,
	[property: JsonPropertyName("email")] string Email,
	[property: JsonPropertyName("token")] string Token);

internal sealed record SessionResponse(
	[property: JsonPropertyName("access_token")] string? AccessToken,
	[property: JsonPropertyName("refresh_token")] string? RefreshToken,
	[property: JsonPropertyName("expires_in")] int ExpiresIn);

internal sealed record AuthErrorResponse(
	[property: JsonPropertyName("msg")] string? Msg,
	[property: JsonPropertyName("message")] string? Message,
	[property: JsonPropertyName("error")] string? Error,
	[property: JsonPropertyName("error_description")] string? ErrorDescription,
	[property: JsonPropertyName("error_code")] string? ErrorCode);

[JsonSerializable(typeof(PkceExchangeRequest))]
[JsonSerializable(typeof(RefreshRequest))]
[JsonSerializable(typeof(SendOtpRequest))]
[JsonSerializable(typeof(VerifyOtpRequest))]
[JsonSerializable(typeof(SessionResponse))]
[JsonSerializable(typeof(AuthErrorResponse))]
internal sealed partial class SupabaseAuthJsonContext : JsonSerializerContext
{
}
