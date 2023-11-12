using MZikmund.Models.Dialogs;

namespace MZikmund.Services.Dialogs;

public interface IDialogService
{
	Task<ContentDialogResult> ShowStatusMessageAsync(
		StatusMessageDialogType type,
		string title,
		string text);

	Task<ContentDialogResult> ShowAsync<TViewModel>(TViewModel viewModel);

	IDialogService Register<TViewModel, TDialog>()
		where TDialog : ContentDialog;
}
