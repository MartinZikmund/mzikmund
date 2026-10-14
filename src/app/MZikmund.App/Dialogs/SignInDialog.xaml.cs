using MZikmund.ViewModels;

namespace MZikmund.Dialogs;

public sealed partial class SignInDialog : SignInDialogBase
{
	public SignInDialog() => InitializeComponent();
}

public abstract partial class SignInDialogBase : DialogBase<SignInDialogViewModel>
{
}
