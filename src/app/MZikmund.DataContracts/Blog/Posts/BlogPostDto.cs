using MZikmund.DataContracts.Blog.Categories;
using MZikmund.DataContracts.Blog.Tags;
using MZikmund.DataContracts.Enums.Blog;

namespace MZikmund.DataContracts.Blog.Posts;

public class BlogPostDto
{
	public int Id { get; set; }

	public BlogPostType PostType { get; set; }

	public BlogPostContentType ContentType { get; set; }

	public BlogCategoryDto[] Categories { get; set; } = Array.Empty<BlogCategoryDto>();

	public BlogTagDto[] Tags { get; set; } = Array.Empty<BlogTagDto>();

	public BlogPostLocalizationDto[] Localizations { get; set; } = Array.Empty<BlogPostLocalizationDto>();
}
