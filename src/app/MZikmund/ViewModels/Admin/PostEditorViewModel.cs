using MZikmund.DataContracts.Blog;
using MZikmund.Services.Localization;
using MZikmund.ViewModels;

namespace MZikmund.ViewModels.Admin;

public class PostEditorViewModel : PageViewModel
{
	public override string Title => Post?.Title ?? "";

	public Post? Post { get; set; }

	public override void ViewAppeared() => base.ViewAppeared();
}
