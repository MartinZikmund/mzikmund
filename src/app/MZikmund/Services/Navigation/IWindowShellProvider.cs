using MZikmund.ViewModels;

namespace MZikmund.Services.Navigation;

internal interface IWindowShellProvider
{
	WindowShellViewModel ViewModel { get; }

	XamlRoot XamlRoot { get; }
}
