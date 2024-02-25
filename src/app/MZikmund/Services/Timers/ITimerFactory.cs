using Microsoft.UI.Dispatching;

namespace MZikmund.Services.Timers;

public interface ITimerFactory
{
	DispatcherQueueTimer CreateTimer();
}
