namespace MZikmund.ViewModels;

public abstract class DialogViewModel : ViewModelBase
{
	public virtual string Title { get; } = "";

	public virtual void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
	{
	}

	public virtual void OnSecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
	{
	}

	public virtual void OnCloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
	{
	}
}
