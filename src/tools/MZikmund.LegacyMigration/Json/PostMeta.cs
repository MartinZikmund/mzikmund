#nullable disable

using System.Text.Json.Serialization;

namespace MZikmund.LegacyMigration.Json;

internal sealed class PostMeta
{
	[JsonPropertyName("meta_id")]
	public long MetaId { get; set; }

	[JsonPropertyName("post_id")]
	public long PostId { get; set; }

	[JsonPropertyName("meta_key")]
	public string MetaKey { get; set; } = null!;

	[JsonPropertyName("meta_value")]
	public string MetaValue { get; set; } = null!;
}
