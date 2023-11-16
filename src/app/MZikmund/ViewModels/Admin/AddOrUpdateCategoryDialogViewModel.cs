using MZikmund.Models.Dialogs;
using MZikmund.Services.Localization;
using MZikmund.ViewModels;

namespace MZikmund.ViewModels.Admin;

public class AddOrUpdateCategoryDialogViewModel : DialogViewModel
{
	public AddOrUpdateCategoryDialogViewModel(CategoryViewModel blogCategory)
	{
		Category = blogCategory ?? throw new ArgumentNullException(nameof(blogCategory));

		Mode = DialogMode.Edit;
	}

	public AddOrUpdateCategoryDialogViewModel() :
		this(new CategoryViewModel())
	{
		Mode = DialogMode.Add;
	}

	public DialogMode Mode { get; set; } = DialogMode.Add;

	public string SecondaryButtonText => Mode == DialogMode.Edit ? Localizer.Instance.GetString("Delete") : string.Empty;

	public CategoryViewModel Category { get; set; }
}
