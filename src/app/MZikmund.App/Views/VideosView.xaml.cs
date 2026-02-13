using MZikmund.ViewModels;

namespace MZikmund.Views;

public sealed partial class VideosView : VideosViewBase
{
	public VideosView()
	{
		this.InitializeComponent();
	}
}

public partial class VideosViewBase : PageBase<VideosViewModel>
{
}
