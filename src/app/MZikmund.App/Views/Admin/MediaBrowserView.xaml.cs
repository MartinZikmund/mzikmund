using Microsoft.UI.Xaml.Controls;
using MZikmund.ViewModels.Admin;
using Windows.ApplicationModel.DataTransfer;

namespace MZikmund.Views.Admin;

public sealed partial class MediaBrowserView : MediaBrowserViewBase
{
	public MediaBrowserView() => InitializeComponent();

	private void CopyUrl_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
	{
		if (sender is Button button && button.Tag is string url)
		{
			var dataPackage = new DataPackage();
			dataPackage.SetText(url);
			Clipboard.SetContent(dataPackage);
		}
	}
}

public partial class MediaBrowserViewBase : PageBase<MediaBrowserViewModel>
{
}
