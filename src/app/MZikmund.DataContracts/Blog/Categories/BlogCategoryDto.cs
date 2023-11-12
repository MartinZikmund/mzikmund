using System;

namespace MZikmund.DataContracts.Blog.Categories;

public class BlogCategoryDto
{
	public int Id { get; set; }

	public string? Icon { get; set; }

	public string DisplayName { get; set; } = "";

	public string RouteName { get; set; } = "";
}
