using System.Reflection;
using MZikmund.Models.Dialogs;
using MZikmund.Services.Localization;
using MZikmund.ViewModels.Dialogs;

namespace MZikmund.Services.Dialogs;

public class DialogService : IDialogService
{
	private readonly Dictionary<string, Type> _dialogs = new();
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

	public Task<ContentDialogResult> ShowConfirmationDialogAsync(string title, string text)
	{
		var confirmationDialog = new ContentDialog();
		confirmationDialog.Title = title;
		confirmationDialog.Content = text;
		confirmationDialog.PrimaryButtonText = Localizer.Instance.GetString("Ok");
		confirmationDialog.CloseButtonText = Localizer.Instance.GetString("Cancel");
		confirmationDialog.DefaultButton = ContentDialogButton.Close;
		return _dialogCoordinator.ShowAsync(confirmationDialog);
	}

	public async Task<ContentDialogResult> ShowAsync<TViewModel>(TViewModel viewModel)
	{
		var viewModelType = typeof(TViewModel);
		if (!viewModelType.Name.EndsWith("ViewModel", StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidOperationException("ViewModel name must end with 'ViewModel' by convention.");
		}

		var viewModelName = viewModelType.Name;
		var dialogTypeName = viewModelName.Substring(0, viewModelName.Length - "ViewModel".Length);
		if (!_dialogs.TryGetValue(dialogTypeName, out var dialogType))
		{
			throw new InvalidOperationException($"Dialog for {viewModelType} not found");
		}
		var dialog = (ContentDialog?)Activator.CreateInstance(dialogType);
		if (dialog is null)
		{
			throw new InvalidOperationException($"Instance of {dialogType} could not be created");
		}
		dialog.DataContext = viewModel;
		return await _dialogCoordinator.ShowAsync(dialog);
	}

	public void RegisterDialogsFromAssembly(Assembly sourceAssembly)
	{
		// TODO: Avoid reflection
		var dialogType = typeof(ContentDialog);
		var pages = sourceAssembly.GetTypes().Where(t => dialogType.IsAssignableFrom(t) && t.Name.EndsWith("Dialog", StringComparison.OrdinalIgnoreCase)).ToArray();
		foreach (var viewType in pages)
		{
			_dialogs.Add(viewType.Name, viewType);
		}
	}
}
