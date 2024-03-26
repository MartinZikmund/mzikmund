using Microsoft.UI.Dispatching;
using MZikmund.ViewModels;

namespace MZikmund.Services.Navigation;

public interface IWindowShellProvider
{
	WindowShellViewModel ViewModel { get; }

	WindowShell WindowShell { get; }

	XamlRoot XamlRoot { get; }

	IServiceProvider ServiceProvider { get; }

	DispatcherQueue DispatcherQueue { get; }
}
