using MZikmund.Dialogs;
using MZikmund.ViewModels.Admin;

namespace MZikmund.App.Dialogs.Admin;

public sealed partial class ImageVariantsDialog : ImageVariantsDialogBase
{
	public ImageVariantsDialog()
	{
		InitializeComponent();
	}
}

public abstract partial class ImageVariantsDialogBase : DialogBase<ImageVariantsDialogViewModel>
{
}
