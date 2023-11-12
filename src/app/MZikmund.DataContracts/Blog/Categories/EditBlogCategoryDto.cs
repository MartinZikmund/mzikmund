using System;

namespace MZikmund.DataContracts.Blog.Categories;

public class EditBlogCategoryDto
{
	public string? Icon { get; set; }

	public string DisplayName { get; set; } = "";

	public string RouteName { get; set; } = "";

	public EditBlogCategoryLocalizationDto[] Localizations { get; set; } = Array.Empty<EditBlogCategoryLocalizationDto>();
}
