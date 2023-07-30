using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace MZikmund.LegacyMigration.Json;

internal sealed class TermRelationship
{
	[JsonProperty("object_id")]
	public long ObjectId { get; set; }

	[JsonProperty("term_taxonomy_id")]
	public long TermTaxonomyId { get; set; }

	[JsonProperty("term_order")]
	public int TermOrder { get; set; }
}
