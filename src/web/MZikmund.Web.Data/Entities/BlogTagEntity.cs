﻿namespace MZikmund.Web.Data.Entities;

public class BlogTagEntity
{
	public Guid Id { get; set; }

	public string DisplayName { get; set; } = "";

	public string? Description { get; set; }

	public string RouteName { get; set; } = "";

	public virtual ICollection<BlogPostEntity> Posts { get; set; }
}