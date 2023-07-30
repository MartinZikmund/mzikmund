#nullable disable

using Newtonsoft.Json;

namespace MZikmund.LegacyMigration.Json;

internal sealed class Post
{
	[JsonProperty("ID")]
	public long Id { get; set; }

	[JsonProperty("post_author")]
	public long PostAuthor { get; set; }

	[JsonProperty("post_date")]
	public string PostDate { get; set; }

	[JsonProperty("post_date_gmt")]
	public string PostDateGmt { get; set; }

	[JsonProperty("post_content")]
	public string PostContent { get; set; } = null!;

	[JsonProperty("post_title")]
	public string PostTitle { get; set; } = null!;

	[JsonProperty("post_excerpt")]
	public string PostExcerpt { get; set; } = null!;

	[JsonProperty("post_status")]
	public string PostStatus { get; set; } = null!;

	[JsonProperty("comment_status")]
	public string CommentStatus { get; set; } = null!;

	[JsonProperty("ping_status")]
	public string PingStatus { get; set; } = null!;

	[JsonProperty("post_password")]
	public string PostPassword { get; set; } = null!;

	[JsonProperty("post_name")]
	public string PostName { get; set; } = null!;

	[JsonProperty("to_ping")]
	public string ToPing { get; set; } = null!;

	[JsonProperty("pinged")]
	public string Pinged { get; set; } = null!;

	[JsonProperty("post_modified")]
	public string PostModified { get; set; }

	[JsonProperty("post_modified_gmt")]
	public string PostModifiedGmt { get; set; }

	[JsonProperty("post_content_filtered")]
	public string PostContentFiltered { get; set; } = null!;

	[JsonProperty("post_parent")]
	public long PostParent { get; set; }

	[JsonProperty("guid")]
	public string Guid { get; set; } = null!;

	[JsonProperty("menu_order")]
	public int MenuOrder { get; set; }

	[JsonProperty("post_type")]
	public string PostType { get; set; } = null!;

	[JsonProperty("post_mime_type")]
	public string PostMimeType { get; set; } = null!;

	[JsonProperty("comment_count")]
	public long CommentCount { get; set; }
}
