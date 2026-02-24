using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using MZikmund.App.Core.ViewModels.Admin;
using MZikmund.Dialogs;
using MZikmund.ViewModels.Admin;
using MZikmund.ViewModels.Items;

namespace MZikmund.App.Dialogs.Admin;

public sealed partial class MediaBrowserDialog : MediaBrowserDialogBase
{
	public MediaBrowserDialog()
	{
		InitializeComponent();
	}

	private void FileCard_Tapped(object sender, TappedRoutedEventArgs e)
	{
		if (sender is Grid grid && grid.Tag is StorageItemInfoViewModel file)
		{
			ViewModel!.SelectedFile = file;
		}
	}
}

public abstract partial class MediaBrowserDialogBase : DialogBase<MediaBrowserDialogViewModel>
{
}
