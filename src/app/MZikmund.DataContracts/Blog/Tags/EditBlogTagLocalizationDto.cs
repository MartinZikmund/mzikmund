namespace MZikmund.DataContracts.Blog.Tags;

public class EditBlogTagLocalizationDto
{
	public int LanguageId { get; set; }

	public string DisplayName { get; set; } = "";

	public string RouteName { get; set; } = "";
}
