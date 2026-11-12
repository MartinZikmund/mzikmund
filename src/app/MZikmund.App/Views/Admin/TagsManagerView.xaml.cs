using MZikmund.DataContracts.Blog;
using MZikmund.ViewModels.Admin;

namespace MZikmund.Views.Admin;

public sealed partial class TagsManagerView : TagsManagerViewBase
{
	public TagsManagerView() => InitializeComponent();

	private void TagPill_Click(object sender, RoutedEventArgs e)
	{
		if (sender is FrameworkElement { Tag: Tag tag })
		{
			ViewModel?.UpdateTagCommand.Execute(tag);
		}
	}
}

public partial class TagsManagerViewBase : PageBase<TagsManagerViewModel>
{
}
