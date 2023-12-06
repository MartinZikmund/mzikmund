using MZikmund.DataContracts.Blog;
using MZikmund.ViewModels.Admin;

namespace MZikmund.Dialogs.Admin;

public sealed partial class CategoryPickerDialog : CategoryPickerDialogBase
{
	public CategoryPickerDialog()
	{
		InitializeComponent();
	}

	internal void SetSelectedItems(IEnumerable<Category> itemsToSelect)
	{
		foreach (var category in itemsToSelect)
		{
			CategoriesListView.SelectedItems.Add(category);
		}
	}

	private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (ViewModel is null)
		{
			return;
		}

		ViewModel.SelectedCategories = CategoriesListView.SelectedItems.Cast<Category>().ToArray();
	}
}

public abstract partial class CategoryPickerDialogBase : DialogBase<CategoryPickerDialogViewModel>
{
}
