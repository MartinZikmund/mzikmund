using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace MZikmund.LegacyMigration.Json;

internal sealed class TermTaxonomy
{
	[JsonProperty("term_taxonomy_id")]
	public long TermTaxonomyId { get; set; }

	[JsonProperty("term_id")]
	public long TermId { get; set; }

	[JsonProperty("taxonomy")]
	public string Taxonomy { get; set; } = null!;

	[JsonProperty("description")]
	public string Description { get; set; } = null!;

	[JsonProperty("parent")]
	public long Parent { get; set; }

	[JsonProperty("count")]
	public long Count { get; set; }
}
