
using System.Diagnostics.CodeAnalysis;
using MZikmund.ViewModels;

namespace MZikmund.Services.Navigation;

internal sealed class WindowShellProvider : IWindowShellProvider
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
			return _shell.ViewModel;
		}
	}

	public XamlRoot XamlRoot
	{
		get
		{
			EnsureInitialized();
			return _shell.XamlRoot!;
		}
	}

	public WindowShell WindowShell
	{
		get
		{
			EnsureInitialized();
			return _shell;
		}
	}

	public IServiceProvider ServiceProvider
	{
		get
		{
			EnsureInitialized();
			return _shell.ServiceProvider;
		}
	}

	[MemberNotNull(nameof(_shell))]
	private void EnsureInitialized()
	{
		if (_shell is null)
		{
			throw new InvalidOperationException("WindowShellProvider was not initialized.");
		}
	}
}
