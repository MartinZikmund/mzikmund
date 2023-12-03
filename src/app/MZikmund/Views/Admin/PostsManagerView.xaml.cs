using MZikmund.DataContracts.Blog;
using MZikmund.ViewModels.Admin;

namespace MZikmund.Views.Admin;

public sealed partial class PostsManagerView : PostsManagerViewBase
{
	public PostsManagerView() => InitializeComponent();

	private void ListView_ItemClick(object sender, ItemClickEventArgs e)
	{
		var post = (PostListItem)e.ClickedItem;
		ViewModel!.UpdatePostCommand.Execute(post);
	}
}

public partial class PostsManagerViewBase : PageBase<PostsManagerViewModel>
{
}
