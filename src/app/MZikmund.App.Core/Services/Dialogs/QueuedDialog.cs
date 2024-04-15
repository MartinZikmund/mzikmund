namespace MZikmund.Services.Dialogs;

public class QueuedDialog
{
	public QueuedDialog(ContentDialog dialog)
	{
		Dialog = dialog;
	}

	public TaskCompletionSource<ContentDialogResult> CompletionSource { get; } = new TaskCompletionSource<ContentDialogResult>();

	public ContentDialog Dialog { get; }
}
