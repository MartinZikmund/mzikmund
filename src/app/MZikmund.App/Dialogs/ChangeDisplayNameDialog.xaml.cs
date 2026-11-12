using MZikmund.ViewModels;

namespace MZikmund.Dialogs;

public sealed partial class ChangeDisplayNameDialog : ChangeDisplayNameDialogBase
{
	public ChangeDisplayNameDialog() => InitializeComponent();
}

public abstract partial class ChangeDisplayNameDialogBase : DialogBase<ChangeDisplayNameDialogViewModel>
{
}
