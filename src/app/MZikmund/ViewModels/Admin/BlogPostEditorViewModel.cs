using MZikmund.Services.Localization;
using MZikmund.ViewModels.Abstract;

namespace MZikmund.ViewModels.Admin;

public class BlogPostEditorViewModel : PageViewModel
{
	private string _title = string.Empty;

	public override string Title => _title;
}
