using System.Text.Json.Serialization;

namespace MZikmund.Web.Core.Services.Comments;

/// <summary>
/// Shape of the Cloudflare siteverify response. Wire names are hyphenated / snake_cased,
/// so the JsonPropertyName attributes cannot be replaced by a naming policy.
/// </summary>
internal sealed class TurnstileSiteVerifyResponse
{
	[JsonPropertyName("success")]
	public bool Success { get; set; }

	[JsonPropertyName("challenge_ts")]
	public DateTimeOffset? ChallengeTimestamp { get; set; }

	[JsonPropertyName("hostname")]
	public string? Hostname { get; set; }

	[JsonPropertyName("error-codes")]
	public string[]? ErrorCodes { get; set; }

	[JsonPropertyName("action")]
	public string? Action { get; set; }

	[JsonPropertyName("cdata")]
	public string? CData { get; set; }
}
