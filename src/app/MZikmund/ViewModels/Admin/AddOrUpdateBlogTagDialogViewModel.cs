using MZikmund.Models.Dialogs;
using MZikmund.Services.Localization;
using MZikmund.ViewModels;

namespace MZikmund.ViewModels.Admin;

public class AddOrUpdateBlogTagDialogViewModel : DialogViewModel
{
	public AddOrUpdateBlogTagDialogViewModel(BlogTagViewModel blogTag)
	{
		BlogTag = blogTag ?? throw new ArgumentNullException(nameof(blogTag));

		Mode = DialogMode.Edit;
	}

	public AddOrUpdateBlogTagDialogViewModel() :
		this(new BlogTagViewModel())
	{
		Mode = DialogMode.Add;
	}

	public DialogMode Mode { get; set; } = DialogMode.Add;

	public string SecondaryButtonText => Mode == DialogMode.Edit ? Localizer.Instance.GetString("Delete") : string.Empty;

	public BlogTagViewModel BlogTag { get; set; }
}
