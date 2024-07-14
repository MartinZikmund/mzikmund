using MZikmund.ViewModels.Admin;

namespace MZikmund.Dialogs.Admin;

public sealed partial class AddOrUpdateTagDialog : AddOrUpdateTagDialogBase
{
	public AddOrUpdateTagDialog() => InitializeComponent();
}

public abstract partial class AddOrUpdateTagDialogBase : DialogBase<AddOrUpdateTagDialogViewModel>
{
}
