using MZikmund.DataContracts.Blog;

namespace MZikmund.App.Core.ViewModels.Admin;

public interface ICategoryPickerDialog
{
	void SetSelectedItems(IEnumerable<Category> categories);
}
