using MZikmund.DataContracts.Blog;
using MZikmund.ViewModels.Admin;

namespace MZikmund.Views.Admin;

public sealed partial class CategoriesManagerView : CategoriesManagerViewBase
{
	public CategoriesManagerView() => InitializeComponent();

	private void CategoryRow_Click(object sender, RoutedEventArgs e)
	{
		if (sender is FrameworkElement { Tag: Category category })
		{
			ViewModel?.UpdateCategoryCommand.Execute(category);
		}
	}
}

public partial class CategoriesManagerViewBase : PageBase<CategoriesManagerViewModel>
{
}
