using MZikmund.ViewModels;
using MZikmund.ViewModels.Items;
using Uno.Extensions;

namespace MZikmund.Views;

public sealed partial class BlogView : BlogViewBase
{
	private const double LoadMoreThreshold = 400;

	private bool _isLoadMoreCheckQueued;

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

	private void OnScrollMetricChanged(DependencyObject sender, DependencyProperty dp) => QueueLoadMoreCheck();

	private void Scroller_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e) => QueueLoadMoreCheck();

	/// <summary>
	/// Queues the paging check for after the current layout pass. The extent and viewport callbacks
	/// run synchronously inside ScrollViewer's arrange, where ScrollableHeight still holds the
	/// previous pass's value, so reading the metrics there reports every first render as scrolled
	/// to the end. Both callbacks can also fire several times per pass, hence the single pending
	/// check.
	/// </summary>
	private void QueueLoadMoreCheck()
	{
		if (_isLoadMoreCheckQueued)
		{
			return;
		}

		_isLoadMoreCheckQueued = true;
		if (!DispatcherQueue.TryEnqueue(RunLoadMoreCheck))
		{
			_isLoadMoreCheckQueued = false;
		}
	}

	// The dispatcher takes a void delegate, so nothing observes the task: an exception escaping
	// here would tear the process down instead of failing a load.
	private async void RunLoadMoreCheck()
	{
		_isLoadMoreCheckQueued = false;
		try
		{
			await TryLoadMoreAsync();
		}
		catch (Exception ex)
		{
			this.Log().LogError(ex, "Checking whether the next page of posts is needed failed.");
		}
	}

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
