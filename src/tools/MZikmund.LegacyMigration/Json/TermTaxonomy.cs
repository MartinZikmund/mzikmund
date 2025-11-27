#nullable disable

using System.Text.Json.Serialization;

namespace MZikmund.LegacyMigration.Json;

internal sealed class TermTaxonomy
{
	[JsonPropertyName("term_taxonomy_id")]
	public long TermTaxonomyId { get; set; }

	[JsonPropertyName("term_id")]
	public long TermId { get; set; }

	[JsonPropertyName("taxonomy")]
	public string Taxonomy { get; set; } = null!;

	[JsonPropertyName("description")]
	public string Description { get; set; } = null!;

	[JsonPropertyName("parent")]
	public long Parent { get; set; }

	[JsonPropertyName("count")]
	public long Count { get; set; }
}
