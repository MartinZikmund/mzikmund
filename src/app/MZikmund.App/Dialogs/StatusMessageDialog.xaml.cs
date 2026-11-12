using MZikmund.Models.Dialogs;
using MZikmund.ViewModels.Dialogs;

namespace MZikmund.Dialogs;

public sealed partial class StatusMessageDialog : StatusMessageDialogBase
{
	public StatusMessageDialog()
	{
		InitializeComponent();
	}

	public static Visibility IsType(StatusMessageDialogType type, StatusMessageDialogType expected) =>
		type == expected ? Visibility.Visible : Visibility.Collapsed;
}

public abstract partial class StatusMessageDialogBase : DialogBase<StatusMessageDialogViewModel>
{
}
