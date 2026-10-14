using MZikmund.ViewModels;
using MZikmund.ViewModels.Items;

namespace MZikmund.Views;

public sealed partial class VideosView : VideosViewBase
{
	public VideosView()
	{
		this.InitializeComponent();
	}

	private async void VideoCard_Click(object sender, RoutedEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: VideoCardViewModel video })
		{
			await VideosViewModel.OpenVideoAsync(video);
		}
	}
}

public partial class VideosViewBase : PageBase<VideosViewModel>
{
}
