using Microsoft.UI.Xaml.Input;
using MZikmund.DataContracts.Blog;
using MZikmund.ViewModels;

namespace MZikmund.Views;

public sealed partial class BlogView : BlogViewBase
{
	public BlogView()
	{
		this.InitializeComponent();
	}

	private void PostCard_PointerPressed(object sender, PointerRoutedEventArgs e)
	{
		if (sender is FrameworkElement element && element.Tag is PostListItem post)
		{
			ViewModel!.NavigateToPost(post.Id);
		}
	}

	private void PostCard_PointerEntered(object sender, PointerRoutedEventArgs e)
	{
		// Intentionally left blank; add visual feedback here if desired.
	}

	private void PostCard_PointerExited(object sender, PointerRoutedEventArgs e) { }
}

public partial class BlogViewBase : PageBase<BlogViewModel>
{
}
