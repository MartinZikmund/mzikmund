namespace MZikmund.Services.Dialogs;

public interface IDialogCoordinator
{
	Task<ContentDialogResult> ShowAsync(ContentDialog dialog);
}
