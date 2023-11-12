using System;

namespace MZikmund.DataContracts.Blog.Tags;

public class EditBlogTagDto
{
	public string DisplayName { get; set; } = "";

	public string RouteName { get; set; } = "";

	public EditBlogTagLocalizationDto[] Localizations { get; set; } = Array.Empty<EditBlogTagLocalizationDto>();
}
