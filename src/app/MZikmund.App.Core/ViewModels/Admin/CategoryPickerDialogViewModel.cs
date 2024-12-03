using MZikmund.Api.Client;
using MZikmund.App.Core.ViewModels.Admin;
using MZikmund.DataContracts.Blog;

namespace MZikmund.ViewModels.Admin;

public partial class CategoryPickerDialogViewModel : DialogViewModel
{
	private readonly Guid[] _initialSelection;
	private readonly IMZikmundApi _api;

	public CategoryPickerDialogViewModel(Guid[] selectedCategoryIds, IMZikmundApi api)
	{
		_initialSelection = selectedCategoryIds;
		_api = api;
	}

	[ObservableProperty]
	public Category[] _allCategories = Array.Empty<Category>();

	[ObservableProperty]
	public Category[] _selectedCategories = Array.Empty<Category>();

	public override async void OnOpened(ContentDialog contentDialog)
	{
		try
		{
			var dialog = (ICategoryPickerDialog)contentDialog;
			AllCategories = (await _api.GetCategoriesAsync()).Content!;
			dialog.SetSelectedItems(AllCategories.Where(c => _initialSelection.Contains(c.Id)));
		}
		catch (Exception)
		{
			// TODO: Handle dialog exception
		}
	}
}
