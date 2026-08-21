using MZikmund.ViewModels;
using MZikmund.ViewModels.Items;

namespace MZikmund.Views;

public sealed partial class BlogView : BlogViewBase
{
	private const double LoadMoreThreshold = 400;

	public BlogView()
	{
		this.InitializeComponent();

		// ViewChanged only fires on offset and zoom changes, so a first page that fits the viewport
		// would never page again. The extent and viewport callbacks cover that case.
		Scroller.RegisterPropertyChangedCallback(ScrollViewer.ExtentHeightProperty, OnScrollMetricChanged);
		Scroller.RegisterPropertyChangedCallback(ScrollViewer.ViewportHeightProperty, OnScrollMetricChanged);
	}

	private void PostCard_Click(object sender, RoutedEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: PostListItemViewModel item })
		{
			ViewModel?.NavigateToPost(item.Item.Id);
		}
	}

	private async void OnScrollMetricChanged(DependencyObject sender, DependencyProperty dp) => await TryLoadMoreAsync();

	private async void Scroller_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e) => await TryLoadMoreAsync();

	private async Task TryLoadMoreAsync()
	{
		if (ViewModel is not { } viewModel)
		{
			return;
		}

		// A failed page waits for the explicit retry rather than firing again on every scroll tick.
		if (viewModel.IsLoading || viewModel.HasError || viewModel.HasLoadMoreError || !viewModel.Posts.HasMoreItems)
		{
			return;
		}

		if (Scroller.ScrollableHeight - Scroller.VerticalOffset > LoadMoreThreshold)
		{
			return;
		}

		await viewModel.Posts.LoadMoreItemsAsync(0);
	}
}

public partial class BlogViewBase : PageBase<BlogViewModel>
{
}
