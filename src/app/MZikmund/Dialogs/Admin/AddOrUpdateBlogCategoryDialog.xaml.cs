using MZikmund.ViewModels.Admin;

namespace MZikmund.Dialogs.Admin;

public sealed partial class AddOrUpdateBlogCategoryDialog : AddOrUpdateBlogCategoryDialogBase
{
	public AddOrUpdateBlogCategoryDialog() => InitializeComponent();
}

public abstract partial class AddOrUpdateBlogCategoryDialogBase : DialogBase<AddOrUpdateBlogCategoryDialogViewModel>
{
}
