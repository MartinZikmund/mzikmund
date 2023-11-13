using MZikmund.ViewModels.Admin;

namespace MZikmund.Views.Admin;

public sealed partial class BlogPostsManagerView : BlogPostsManagerViewBase
{
	public BlogPostsManagerView() => InitializeComponent();
}

public partial class BlogPostsManagerViewBase : PageBase<BlogPostsManagerViewModel>
{
}
