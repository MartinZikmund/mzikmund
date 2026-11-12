using MZikmund.Api.Client;
using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Services.Account;
using MZikmund.Services.Localization;

namespace MZikmund.ViewModels;

public partial class ChangeDisplayNameDialogViewModel : DialogViewModel
{
	private readonly IMZikmundApi _api;
	private ContentDialog? _dialog;

	public ChangeDisplayNameDialogViewModel(IMZikmundApi api, string currentName)
	{
		_api = api ?? throw new ArgumentNullException(nameof(api));
		DisplayName = currentName;
	}

	public override string Title => Localizer.Instance.GetString("ChangeNameTitle");

	public int MaxDisplayNameLength => CommentLimits.MaxDisplayNameLength;

	public bool Saved { get; private set; }

	public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

	[ObservableProperty]
	public partial string DisplayName { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasError))]
	public partial string? ErrorMessage { get; set; }

	[ObservableProperty]
	public partial bool IsBusy { get; set; }

	public override void OnOpened(ContentDialog contentDialog) => _dialog = contentDialog;

	// The dialog stays open until the save works, so a failed attempt keeps what was typed.
	public override void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
	{
		args.Cancel = true;
		_ = SaveAsync();
	}

	public async Task SaveAsync()
	{
		if (IsBusy)
		{
			return;
		}

		ErrorMessage = null;
		IsBusy = true;
		try
		{
			if (await DisplayNameSaver.SaveAsync(_api, DisplayName) is { } error)
			{
				ErrorMessage = error;
				return;
			}

			Saved = true;
			_dialog?.Hide();
		}
		finally
		{
			IsBusy = false;
		}
	}
}
