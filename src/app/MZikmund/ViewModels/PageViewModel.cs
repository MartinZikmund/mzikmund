namespace MZikmund.ViewModels;

public abstract class PageViewModel : ViewModelBase
{
	//private INavigationService? _navigationService = null!;

	public virtual string Title { get; } = "";

	//protected INavigationService NavigationService
	//{
	//	get
	//	{
	//		_navigationService ??= Ioc.Default.GetRequiredService<INavigationService>();
	//		return _navigationService;
	//	}
	//}

	public virtual void ViewCreated() { }

	public virtual void ViewAppearing() { }

	public virtual void ViewAppeared() { }

	public virtual void ViewDisappeared() { }

	public virtual void ViewDisappearing() { }

	public virtual void ViewDestroy() { }
}
