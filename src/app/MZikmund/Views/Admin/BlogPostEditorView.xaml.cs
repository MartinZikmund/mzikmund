using MZikmund.ViewModels.Admin;

namespace MZikmund.Views.Admin;

public sealed partial class BlogPostEditorView : BlogPostEditorViewBase
{
	public BlogPostEditorView() => InitializeComponent();
}

public partial class BlogPostEditorViewBase : PageBase<BlogPostEditorViewModel>
{
}
