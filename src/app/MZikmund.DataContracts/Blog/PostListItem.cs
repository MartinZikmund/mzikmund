namespace MZikmund.DataContracts.Blog;

public class PostListItem
{
	public Guid Id { get; init; } = Guid.Empty;

	public string RouteName { get; set; } = "";

	public string Title { get; set; } = "";

	public string Content { get; set; } = "";

	public string Abstract { get; set; } = "";

	public DateTimeOffset? PublishedDate { get; set; }

	public DateTimeOffset? LastModifiedDate { get; set; }

	public string? HeroImageUrl { get; set; }

	public string? HeroImageAlt { get; set; }

	public Category[] Categories { get; set; } = Array.Empty<Category>();

	public Tag[] Tags { get; set; } = Array.Empty<Tag>();

	/// <summary>
	/// Only filled by the admin list; null when the server does not report it.
	/// </summary>
	public bool? IsPublished { get; set; }
}
