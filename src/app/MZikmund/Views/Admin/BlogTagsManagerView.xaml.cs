using MZikmund.DataContracts.Blog;
using MZikmund.ViewModels.Admin;

namespace MZikmund.Views.Admin;

public sealed partial class TagsManagerView : TagsManagerViewBase
{
	public TagsManagerView() => InitializeComponent();

	private void GridView_ItemClick(object sender, ItemClickEventArgs e)
	{
		var tag = (Tag)e.ClickedItem;
		ViewModel!.UpdateTagCommand.Execute(tag);
	}
}

public partial class TagsManagerViewBase : PageBase<TagsManagerViewModel>
{
}
