using MZikmund.ViewModels.Admin;

namespace MZikmund.Views.Admin;

public sealed partial class TagsManagerView : TagsManagerViewBase
{
	public TagsManagerView() => InitializeComponent();
}

public partial class TagsManagerViewBase : PageBase<TagsManagerViewModel>
{
}
