namespace MZikmund.Services.Dialogs;

public class DialogCoordinator : IDialogCoordinator
{
	private readonly Queue<QueuedDialog> _dialogQueue = new Queue<QueuedDialog>();

	private bool _isProcessing;

	public async Task<ContentDialogResult> ShowAsync(ContentDialog dialog)
	{
		var queuedDialog = EnqueueDialog(dialog);
		await ProcessQueueAsync();
		return await queuedDialog.CompletionSource.Task;
	}

	private QueuedDialog EnqueueDialog(ContentDialog dialog)
	{
		var queuedDialog = new QueuedDialog(dialog);
		_dialogQueue.Enqueue(queuedDialog);
		return queuedDialog;
	}

	private async Task ProcessQueueAsync()
	{
		if (!_isProcessing)
		{
			try
			{
				_isProcessing = true;
				while (_dialogQueue.Count > 0)
				{
					var queuedDialog = _dialogQueue.Dequeue();
					var result = await queuedDialog.Dialog.ShowAsync();
					queuedDialog.CompletionSource.SetResult(result);
				}
			}
			finally
			{
				_isProcessing = false;
			}
		}
	}
}
