using System.Reflection;

namespace MZikmund.Services.Navigation;

public interface INavigationService
{
	void Navigate<TViewModel>();

	void Navigate<TViewModel>(object parameter);

	bool GoBack();

	void Initialize();

	void RegisterViewsFromAssembly(Assembly sourceAssembly);
}
