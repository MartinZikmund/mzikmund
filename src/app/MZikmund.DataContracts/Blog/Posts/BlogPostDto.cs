namespace MZikmund.DataContracts.Blog.Posts;

public class BlogPostDto
{
	public int Id { get; set; }

	public BlogPostType PostType { get; set; }

	public BlogPostContentType ContentType { get; set; }

	public Category[] Categories { get; set; } = Array.Empty<Category>();

	public Tag[] Tags { get; set; } = Array.Empty<Tag>();

	public BlogPostLocalizationDto[] Localizations { get; set; } = Array.Empty<BlogPostLocalizationDto>();
}
