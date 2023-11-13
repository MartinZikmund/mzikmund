using System.Collections.ObjectModel;

namespace MZikmund.ViewModels.Admin;

public class BlogCategoryViewModel : ObservableObject
{
	public int Id { get; set; }

	public string DisplayName { get; set; } = "";

	public string RouteName { get; set; } = "";
}
