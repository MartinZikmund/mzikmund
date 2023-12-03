using MZikmund.DataContracts.Blog;
using MZikmund.ViewModels.Admin;

namespace MZikmund.Views.Admin;

public sealed partial class CategoriesManagerView : CategoriesManagerViewBase
{
	public CategoriesManagerView() => InitializeComponent();

	private void GridView_ItemClick(object sender, ItemClickEventArgs e)
	{
		var category = (Category)e.ClickedItem;
		ViewModel!.UpdateCategoryCommand.Execute(category);
	}
}

public partial class CategoriesManagerViewBase : PageBase<CategoriesManagerViewModel>
{
}
