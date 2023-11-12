using MZikmund.Models.Dialogs;
using MZikmund.ViewModels.Dialogs;

namespace MZikmund.Services.Dialogs;

public class DialogService : IDialogService
{
	private readonly Dictionary<Type, Type> _viewModelToDialogMap = new();
	private readonly IDialogCoordinator _dialogCoordinator;

	public DialogService(IDialogCoordinator dialogCoordinator)
	{
		_dialogCoordinator = dialogCoordinator ?? throw new ArgumentNullException(nameof(dialogCoordinator));
	}

	public async Task<ContentDialogResult> ShowStatusMessageAsync(
		StatusMessageDialogType type,
		string title,
		string text)
	{
		var statusMessageViewModel = new StatusMessageDialogViewModel(type, title, text);
		return await ShowAsync(statusMessageViewModel);
	}

	public async Task<ContentDialogResult> ShowAsync<TViewModel>(TViewModel viewModel)
	{
		var dialogType = _viewModelToDialogMap[typeof(TViewModel)];
		var dialog = (ContentDialog?)Activator.CreateInstance(dialogType);
		if (dialog is null)
		{
			throw new InvalidOperationException("Could not initialize dialog " + dialogType);
		}
		dialog.DataContext = viewModel;
		return await _dialogCoordinator.ShowAsync(dialog);
	}

	public IDialogService Register<TViewModel, TDialog>()
		where TDialog : ContentDialog
	{
		_viewModelToDialogMap[typeof(TViewModel)] = typeof(TDialog);
		return this;
	}
}
