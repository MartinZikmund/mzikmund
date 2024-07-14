using MZikmund.ViewModels.Dialogs;

namespace MZikmund.Dialogs;

public sealed partial class StatusMessageDialog : StatusMessageDialogBase
{
	public StatusMessageDialog()
	{
		InitializeComponent();
	}
}

public abstract partial class StatusMessageDialogBase : DialogBase<StatusMessageDialogViewModel>
{
}
