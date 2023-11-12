using MZikmund.Services.Localization;
using MZikmund.ViewModels.Abstract;

namespace MZikmund.ViewModels.Admin;

public class BlogPostsManagerViewModel : PageViewModel
{
	public override string Title => Localizer.Instance.GetString("BlogPosts");
}
