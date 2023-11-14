namespace MZikmund.DataContracts.Blog;

public class Category
{
	public Guid Id { get; set; }

	public string DisplayName { get; set; } = "";

	public string? Description { get; set; }

	public string? Icon { get; set; }

	public string RouteName { get; set; } = "";
}
