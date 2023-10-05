﻿namespace MZikmund.Web.Core.Dtos;

public class Post
{
	public Guid Id { get; set; } = Guid.Empty;

	public string RouteName { get; set; } = "";

	public string Title { get; set; } = "";

	public string Content { get; set; } = "";

	public string Abstract { get; set; } = "";

	public string? HeroImageUrl { get; set; }

	public string? HeroImageAlt { get; set; }

	public DateTimeOffset? PublishedDate { get; set; }

	public DateTimeOffset? LastModifiedDate { get; set; }

	public Category[] Categories { get; set; } = Array.Empty<Category>();

	public Tag[] Tags { get; set; } = Array.Empty<Tag>();
}
