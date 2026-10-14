using MZikmund.ViewModels;

namespace MZikmund.Views;

public sealed partial class ContactView : ContactViewBase
{
	public ContactView()
	{
		InitializeComponent();
	}
}

public partial class ContactViewBase : PageBase<ContactViewModel>
{
}
