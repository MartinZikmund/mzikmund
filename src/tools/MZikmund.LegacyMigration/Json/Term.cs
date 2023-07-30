#nullable disable

using Newtonsoft.Json;

namespace MZikmund.LegacyMigration.Json;

internal sealed class Term
{
	[JsonProperty("term_id")]
	public long TermId { get; set; }

	[JsonProperty("name")]
	public string Name { get; set; } = null!;

	[JsonProperty("slug")]
	public string Slug { get; set; } = null!;

	[JsonProperty("term_group")]
	public long TermGroup { get; set; }
}
