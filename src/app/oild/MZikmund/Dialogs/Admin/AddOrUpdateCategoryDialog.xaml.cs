using MZikmund.ViewModels.Admin;

namespace MZikmund.Dialogs.Admin;

public sealed partial class AddOrUpdateCategoryDialog : AddOrUpdateCategoryDialogBase
{
	public AddOrUpdateCategoryDialog() => InitializeComponent();
}

public abstract partial class AddOrUpdateCategoryDialogBase : DialogBase<AddOrUpdateCategoryDialogViewModel>
{
}
