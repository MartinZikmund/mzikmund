using MZikmund.ViewModels;

namespace MZikmund.Views;

public sealed partial class BlogView : BlogViewBase
{
	public BlogView()
	{
		this.InitializeComponent();
	}
}

public partial class BlogViewBase : PageBase<BlogViewModel>
{
}
