using Microsoft.UI;
using MZikmund.Models.Dialogs;
using MZikmund.ViewModels.Abstract;

namespace MZikmund.ViewModels.Dialogs;

public class StatusMessageDialogViewModel : DialogViewModel
{
	private string _title;

	public StatusMessageDialogViewModel(StatusMessageDialogType dialogType, string title, string text)
	{
		var color = dialogType switch
		{
			StatusMessageDialogType.Information => Colors.DeepSkyBlue,
			StatusMessageDialogType.Warning => Colors.DarkOrange,
			StatusMessageDialogType.Error => Colors.DarkRed,
			_ => throw new ArgumentOutOfRangeException(nameof(dialogType), dialogType, null)
		};
		IconForeground = new SolidColorBrush(color);

		IconGlyph = dialogType switch
		{
			StatusMessageDialogType.Information => "\uE946",
			StatusMessageDialogType.Warning => "\uE1DE",
			StatusMessageDialogType.Error => "\uE25B",
			_ => throw new ArgumentOutOfRangeException(nameof(dialogType), dialogType, null)
		};

		_title = title ?? throw new ArgumentNullException(nameof(title));
		Text = text ?? throw new ArgumentNullException(nameof(text));
	}

	public override string Title => _title;

	public Brush IconForeground { get; set; }

	public string IconGlyph { get; set; }

	public string Text { get; set; }
}
