using MZikmund.ViewModels.Admin;

namespace MZikmund.Views.Admin;

public sealed partial class PostEditorView : PostEditorViewBase
{
	public PostEditorView() => InitializeComponent();
}

public partial class PostEditorViewBase : PageBase<PostEditorViewModel>
{
}
