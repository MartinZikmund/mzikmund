using MZikmund.ViewModels.Admin;

namespace MZikmund.Views.Admin;

public sealed partial class CategoriesManagerView : CategoriesManagerViewBase
{
	public CategoriesManagerView() => InitializeComponent();
}

public partial class CategoriesManagerViewBase : PageBase<CategoriesManagerViewModel>
{
}
