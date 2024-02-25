using Microsoft.UI.Dispatching;
using MZikmund.Services.Navigation;

namespace MZikmund.Services.Timers;

public class TimerFactory : ITimerFactory
{
	private readonly IWindowShellProvider _windowShellProvider;

	public TimerFactory(IWindowShellProvider windowShellProvider)
	{
		_windowShellProvider = windowShellProvider ?? throw new ArgumentNullException(nameof(windowShellProvider));
	}

	public DispatcherQueueTimer CreateTimer() => _windowShellProvider.WindowShell.DispatcherQueue.CreateTimer();
}
