#nullable disable

using System.Text.Json.Serialization;

namespace MZikmund.LegacyMigration.Json;

internal sealed class TermRelationship
{
	[JsonPropertyName("object_id")]
	public long ObjectId { get; set; }

	[JsonPropertyName("term_taxonomy_id")]
	public long TermTaxonomyId { get; set; }

	[JsonPropertyName("term_order")]
	public int TermOrder { get; set; }
}
