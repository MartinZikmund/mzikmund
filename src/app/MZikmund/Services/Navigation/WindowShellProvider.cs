
using MZikmund.ViewModels;

namespace MZikmund.Services.Navigation;

internal class WindowShellProvider : IWindowShellProvider
{
	private WindowShell? _shell;

	public WindowShellProvider()
	{
	}

	public void SetShell(WindowShell shell)
	{
		if (shell is null)
		{
			throw new ArgumentNullException(nameof(shell));
		}

		_shell = shell;
	}

	public WindowShellViewModel ViewModel
	{
		get
		{
			EnsureInitialized();
			return _shell!.ViewModel;
		}
	}

	public XamlRoot XamlRoot
	{
		get
		{
			EnsureInitialized();
			return _shell!.XamlRoot!;
		}
	}

	private void EnsureInitialized()
	{
		if (XamlRoot is null)
		{
			throw new InvalidOperationException("WindowShellProvider was not initialized.");
		}
	}
}
