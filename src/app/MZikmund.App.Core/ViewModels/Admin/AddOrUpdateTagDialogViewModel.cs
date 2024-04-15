using MZikmund.Models.Dialogs;
using MZikmund.Services.Localization;

namespace MZikmund.ViewModels.Admin;

public class AddOrUpdateTagDialogViewModel : DialogViewModel
{
	public AddOrUpdateTagDialogViewModel(TagViewModel blogTag)
	{
		Tag = blogTag ?? throw new ArgumentNullException(nameof(blogTag));

		Mode = DialogMode.Edit;
	}

	public AddOrUpdateTagDialogViewModel() :
		this(new TagViewModel())
	{
		Mode = DialogMode.Add;
	}

	public override string Title => Localizer.Instance.GetString($"{Mode}Tag");

	public DialogMode Mode { get; set; } = DialogMode.Add;

	public bool IsEditing => Mode == DialogMode.Edit;

	public string SecondaryButtonText => Mode == DialogMode.Edit ? Localizer.Instance.GetString("Delete") : string.Empty;

	public TagViewModel Tag { get; set; }
}
