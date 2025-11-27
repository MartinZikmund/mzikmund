#nullable disable

using System.Text.Json.Serialization;

namespace MZikmund.LegacyMigration.Json;

internal sealed class Term
{
	[JsonPropertyName("term_id")]
	public long TermId { get; set; }

	[JsonPropertyName("name")]
	public string Name { get; set; } = null!;

	[JsonPropertyName("slug")]
	public string Slug { get; set; } = null!;

	[JsonPropertyName("term_group")]
	public long TermGroup { get; set; }
}
