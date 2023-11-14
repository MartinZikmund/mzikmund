namespace MZikmund.DataContracts.Blog;

public class EditCategory
{
	public string DisplayName { get; set; } = "";

	public string RouteName { get; set; } = "";

	public string? Description { get; set; }

	public string? Icon { get; set; }
}
