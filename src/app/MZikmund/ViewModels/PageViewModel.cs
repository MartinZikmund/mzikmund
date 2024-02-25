
namespace MZikmund.ViewModels;

public abstract class PageViewModel : ViewModelBase
{
	public virtual string Title { get; } = "";

	public virtual void ViewCreated() { }

	public virtual void ViewLoading() { }

	public virtual void ViewLoaded() { }

	public virtual void ViewUnloaded() { }

	public virtual void ViewNavigatedTo(object parameter) { }
}
