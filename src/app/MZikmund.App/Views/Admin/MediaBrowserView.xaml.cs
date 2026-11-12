using MZikmund.ViewModels.Admin;
using MZikmund.ViewModels.Items;
using Windows.ApplicationModel.DataTransfer;

namespace MZikmund.Views.Admin;

public sealed partial class MediaBrowserView : MediaBrowserViewBase
{
	public MediaBrowserView()
	{
		InitializeComponent();
	}

	// Flyout items live in a popup, outside the page's name scope, so they carry their item in Tag.
	private void CopyUrl_Click(object sender, RoutedEventArgs e)
	{
		if (sender is FrameworkElement { Tag: Uri url })
		{
			var dataPackage = new DataPackage();
			dataPackage.SetText(url.AbsoluteUri);
			Clipboard.SetContent(dataPackage);
		}
	}

	private void ShowVariants_Click(object sender, RoutedEventArgs e)
	{
		if (sender is FrameworkElement { Tag: StorageItemInfoViewModel file })
		{
			ViewModel?.ShowVariantsCommand.Execute(file);
		}
	}

	private void DeleteFile_Click(object sender, RoutedEventArgs e)
	{
		if (sender is FrameworkElement { Tag: StorageItemInfoViewModel file })
		{
			ViewModel?.DeleteFileCommand.Execute(file);
		}
	}

	public static bool Not(bool value) => !value;
}

public partial class MediaBrowserViewBase : PageBase<MediaBrowserViewModel>
{
}
