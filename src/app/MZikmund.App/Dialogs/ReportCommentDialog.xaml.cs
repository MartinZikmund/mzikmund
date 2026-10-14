using MZikmund.ViewModels;

namespace MZikmund.Dialogs;

public sealed partial class ReportCommentDialog : ReportCommentDialogBase
{
	public ReportCommentDialog() => InitializeComponent();
}

public abstract partial class ReportCommentDialogBase : DialogBase<ReportCommentDialogViewModel>
{
}
