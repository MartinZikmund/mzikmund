using MZikmund.ViewModels.Admin;

namespace MZikmund.Views.Admin;

public sealed partial class BlogCategoriesManagerView : BlogCategoriesManagerViewBase
{
	public BlogCategoriesManagerView() => InitializeComponent();
}

public partial class BlogCategoriesManagerViewBase : PageBase<BlogCategoriesManagerViewModel>
{
}
