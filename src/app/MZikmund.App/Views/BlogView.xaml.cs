using Microsoft.UI.Xaml.Input;
using MZikmund.DataContracts.Blog;
using MZikmund.ViewModels;

namespace MZikmund.Views;

public sealed partial class BlogView : BlogViewBase
{
	public BlogView()
	{
		this.InitializeComponent();
		Scroller.RegisterPropertyChangedCallback(ScrollViewer.ViewportHeightProperty, OnViewPortChanged);
	}

	private void OnViewPortChanged(DependencyObject sender, DependencyProperty dp) => TryLoadMore();

	private void PostCard_PointerPressed(object sender, PointerRoutedEventArgs e)
	{
		if (sender is FrameworkElement element && element.Tag is PostListItem post)
		{
			ViewModel!.NavigateToPost(post.Id);
		}
	}

	private void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
	{
		TryLoadMore();
	}

	private void TryLoadMore()
	{
		double difference = Scroller.ScrollableHeight - Scroller.VerticalOffset;

		if (difference < 100 && ViewModel?.CanLoadMore == true && ViewModel.LoadMoreCommand.CanExecute(null))
		{
			ViewModel.LoadMoreCommand?.Execute(null);
		}
	}
}

public partial class BlogViewBase : PageBase<BlogViewModel>
{
}
