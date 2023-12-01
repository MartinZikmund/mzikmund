namespace MZikmund.Web.Core.Syndication;

public record FeedEntry
{
	public string Id { get; set; } = "";

	public DateTimeOffset PublishedDate { get; set; }

	public DateTimeOffset UpdatedDate { get; set; }

	public string Title { get; set; } = "";

	public string Link { get; set; } = "";

	public string Description { get; set; } = "";

	public string Author { get; set; } = "";

	public string? AuthorEmail { get; set; } = "";

	public string[] Categories { get; set; } = Array.Empty<string>();
}
