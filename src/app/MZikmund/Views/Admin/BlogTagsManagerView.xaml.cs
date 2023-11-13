using MZikmund.ViewModels.Admin;

namespace MZikmund.Views.Admin;

public sealed partial class BlogTagsManagerView : BlogTagsManagerViewBase
{
	public BlogTagsManagerView() => InitializeComponent();
}

public partial class BlogTagsManagerViewBase : PageBase<BlogTagsManagerViewModel>
{
}
