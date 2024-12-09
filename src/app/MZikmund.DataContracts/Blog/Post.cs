namespace MZikmund.DataContracts.Blog;

public class Post
{
	private string _content = "";

	public Guid Id { get; set; } = Guid.Empty;

	public string RouteName { get; set; } = "";

	public string Title { get; set; } = "";

	public string Content
	{
		get => _content.ReplaceLineEndings("\r\n");
		set => _content = value;
	}

	public string Abstract { get; set; } = "";

	public string? HeroImageUrl { get; set; }

	public string? HeroImageAlt { get; set; }

	public DateTimeOffset? PublishedDate { get; set; }

	public DateTimeOffset? LastModifiedDate { get; set; }

	public Category[] Categories { get; set; } = Array.Empty<Category>();

	public Tag[] Tags { get; set; } = Array.Empty<Tag>();

	public string LanguageCode { get; set; } = "en";

	public bool IsPublished { get; set; }
}
