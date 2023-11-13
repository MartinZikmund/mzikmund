using System.Collections.Concurrent;
using Windows.UI.Core;

namespace MZikmund.Helpers;

public class CoreWindowBoundBuilder<T>
{
	private readonly Func<T> _builder;

	private readonly ConcurrentDictionary<CoreWindow, T> _instances = new ConcurrentDictionary<CoreWindow, T>();

	public CoreWindowBoundBuilder(Func<T> builder) => _builder = builder;

	public T GetForCurrentView()
	{
		var window = GetCurrentCoreWindow();
		if (!_instances.TryGetValue(window, out var instance))
		{
			instance = _builder();
			_instances.TryAdd(window, instance);
		}
		return instance;
	}

	private CoreWindow GetCurrentCoreWindow()
	{
		var window = CoreWindow.GetForCurrentThread();
		if (window is null)
		{
			throw new InvalidOperationException("Current thread is not associated with any CoreWindow");
		}
		return window;
	}
}
