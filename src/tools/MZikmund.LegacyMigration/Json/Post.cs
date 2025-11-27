#nullable disable

using System.Text.Json.Serialization;

namespace MZikmund.LegacyMigration.Json;

public sealed class Post
{
	[JsonPropertyName("ID")]
	public long Id { get; set; }

	[JsonPropertyName("post_author")]
	public long PostAuthor { get; set; }

	[JsonPropertyName("post_date")]
	public string PostDate { get; set; }

	[JsonPropertyName("post_date_gmt")]
	public string PostDateGmt { get; set; }

	[JsonPropertyName("post_content")]
	public string PostContent { get; set; } = null!;

	[JsonPropertyName("post_title")]
	public string PostTitle { get; set; } = null!;

	[JsonPropertyName("post_excerpt")]
	public string PostExcerpt { get; set; } = null!;

	[JsonPropertyName("post_status")]
	public string PostStatus { get; set; } = null!;

	[JsonPropertyName("comment_status")]
	public string CommentStatus { get; set; } = null!;

	[JsonPropertyName("ping_status")]
	public string PingStatus { get; set; } = null!;

	[JsonPropertyName("post_password")]
	public string PostPassword { get; set; } = null!;

	[JsonPropertyName("post_name")]
	public string PostName { get; set; } = null!;

	[JsonPropertyName("to_ping")]
	public string ToPing { get; set; } = null!;

	[JsonPropertyName("pinged")]
	public string Pinged { get; set; } = null!;

	[JsonPropertyName("post_modified")]
	public string PostModified { get; set; }

	[JsonPropertyName("post_modified_gmt")]
	public string PostModifiedGmt { get; set; }

	[JsonPropertyName("post_content_filtered")]
	public string PostContentFiltered { get; set; } = null!;

	[JsonPropertyName("post_parent")]
	public long PostParent { get; set; }

	[JsonPropertyName("guid")]
	public string Guid { get; set; } = null!;

	[JsonPropertyName("menu_order")]
	public int MenuOrder { get; set; }

	[JsonPropertyName("post_type")]
	public string PostType { get; set; } = null!;

	[JsonPropertyName("post_mime_type")]
	public string PostMimeType { get; set; } = null!;

	[JsonPropertyName("comment_count")]
	public long CommentCount { get; set; }
}
