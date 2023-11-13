namespace MZikmund.DataContracts.Blog.Categories;

public class EditBlogCategoryLocalizationDto
{
	public int LanguageId { get; set; }

	public string DisplayName { get; set; } = "";

	public string RouteName { get; set; } = "";
}
