#nullable disable

using Newtonsoft.Json;

namespace MZikmund.LegacyMigration.Json;

internal sealed class PostMeta
{
	[JsonProperty("meta_id")]
	public long MetaId { get; set; }

	[JsonProperty("post_id")]
	public long PostId { get; set; }

	[JsonProperty("meta_key")]
	public string MetaKey { get; set; } = null!;

	[JsonProperty("meta_value")]
	public string MetaValue { get; set; } = null!;
}
