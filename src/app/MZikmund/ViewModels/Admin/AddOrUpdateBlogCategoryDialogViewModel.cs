using MZikmund.Models.Dialogs;
using MZikmund.Services.Localization;
using MZikmund.ViewModels;

namespace MZikmund.ViewModels.Admin;

public class AddOrUpdateBlogCategoryDialogViewModel : DialogViewModel
{
	public AddOrUpdateBlogCategoryDialogViewModel(BlogCategoryViewModel blogCategory)
	{
		BlogCategory = blogCategory ?? throw new ArgumentNullException(nameof(blogCategory));

		Mode = DialogMode.Edit;
	}

	public AddOrUpdateBlogCategoryDialogViewModel() :
		this(new BlogCategoryViewModel())
	{
		Mode = DialogMode.Add;
	}

	public DialogMode Mode { get; set; } = DialogMode.Add;

	public string SecondaryButtonText => Mode == DialogMode.Edit ? Localizer.Instance.GetString("Delete") : string.Empty;

	public BlogCategoryViewModel BlogCategory { get; set; }
}
