using MZikmund.Models.Dialogs;

namespace MZikmund.ViewModels.Dialogs;

public class StatusMessageDialogViewModel : DialogViewModel
{
	private readonly string _title;

	public StatusMessageDialogViewModel(StatusMessageDialogType dialogType, string title, string text)
	{
		DialogType = dialogType;
		_title = title ?? throw new ArgumentNullException(nameof(title));
		Text = text ?? throw new ArgumentNullException(nameof(text));
	}

	public override string Title => _title;

	public StatusMessageDialogType DialogType { get; }

	public string Text { get; }
}
