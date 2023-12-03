
namespace MZikmund.ViewModels;

public abstract class PageViewModel : ViewModelBase
{
	public virtual string Title { get; } = "";

	public virtual void ViewCreated() { }

	public virtual void ViewAppearing() { }

	public virtual void ViewAppeared() { }

	public virtual void ViewDisappeared() { }

	public virtual void ViewDisappearing() { }

	public virtual void ViewDestroy() { }

	public virtual void ViewNavigatedTo(object parameter) { }
}
