using System.Reflection;

namespace MZikmund.Services.Navigation;

public interface INavigationService
{
	void Navigate<TViewModel>();

	bool GoBack();

	void Initialize();

	void RegisterViewsFromAssembly(Assembly sourceAssembly);
}
