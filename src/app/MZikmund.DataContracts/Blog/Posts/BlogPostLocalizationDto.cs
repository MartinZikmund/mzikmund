using System;

namespace MZikmund.DataContracts.Blog.Posts;

public class BlogPostLocalizationDto
{
	public string RouteName { get; set; } = "";

	public string Title { get; set; } = "";

	public string? HeroImage { get; set; }

	public string? Perex { get; set; }

	public string? MetaTitle { get; set; }

	public string? MetaDescription { get; set; }

	public string Content { get; set; } = "";

	public DateTimeOffset? PublishDate { get; set; }

	public DateTimeOffset? LastModificationDate { get; set; }

	public int LanguageId { get; set; }
}
