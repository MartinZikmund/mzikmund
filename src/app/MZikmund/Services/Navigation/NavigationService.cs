using System.Reflection;
using Windows.UI.Core;

namespace MZikmund.Services.Navigation;

public class NavigationService : INavigationService
{
	private readonly Dictionary<string, Type> _views = new();
	private readonly IFrameProvider _frameProvider;

	public NavigationService(IFrameProvider frameProvider)
	{
		_frameProvider = frameProvider ?? throw new ArgumentNullException(nameof(frameProvider));
	}

	private Frame Frame => _frameProvider.GetForCurrentView();

	public bool GoBack()
	{
		if (Frame.CanGoBack)
		{
			Frame.GoBack();
			return true;
		}
		return false;
	}

	public void Navigate<TViewModel>()
	{
		if (!TryFindViewForViewModel(typeof(TViewModel), out var viewType))
		{
			throw new InvalidOperationException($"ViewModel type {typeof(TViewModel).Name} is not registered for navigation.");
		}

		Frame.Navigate(viewType);
	}

	private bool TryFindViewForViewModel(Type viewModelType, out Type? viewType)
	{
		if (!viewModelType.Name.EndsWith("ViewModel", StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidOperationException("ViewModel name must end with 'ViewModel' by convention.");
		}

		var viewModelName = viewModelType.Name;
		return _views.TryGetValue(viewModelName.Substring(0, viewModelName.Length - "Model".Length), out viewType);
	}

	public void RegisterViewsFromAssembly(Assembly sourceAssembly)
	{
		// TODO: Avoid reflection
		var pageType = typeof(Page);
		var pages = sourceAssembly.GetTypes().Where(t => pageType.IsAssignableFrom(t) && t.Name.EndsWith("View", StringComparison.OrdinalIgnoreCase)).ToArray();
		foreach (var viewType in pages)
		{
			_views.Add(viewType.Name, viewType);
		}
	}

	public void Initialize() =>
		SystemNavigationManager.GetForCurrentView().BackRequested += NavigationManagerBackRequested;

	private void NavigationManagerBackRequested(object? sender, BackRequestedEventArgs? e) => GoBack();
}
