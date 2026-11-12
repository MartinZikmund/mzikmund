using MZikmund.ViewModels;
using MZikmund.ViewModels.Items;

namespace MZikmund.Views;

public sealed partial class HomeView : HomeViewBase
{
	public HomeView()
	{
		InitializeComponent();
	}

	private void PostCard_Click(object sender, RoutedEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: PostListItemViewModel item })
		{
			ViewModel?.OpenPost(item.Item.Id);
		}
	}

	private async void VideoCard_Click(object sender, RoutedEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: VideoCardViewModel video })
		{
			await HomeViewModel.OpenVideoAsync(video);
		}
	}
}

public partial class HomeViewBase : PageBase<HomeViewModel>
{
}
