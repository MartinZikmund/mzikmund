using MZikmund.App.Core.Infrastructure;

namespace MZikmund.Services.Navigation;

public interface IWindowShellProvider : IWindowShell
{
	Window Window { get; }
}
