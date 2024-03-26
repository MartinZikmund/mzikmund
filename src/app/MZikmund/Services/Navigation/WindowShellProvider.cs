
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Dispatching;
using MZikmund.ViewModels;

namespace MZikmund.Services.Navigation;

internal sealed class WindowShellProvider : IWindowShellProvider
{
	private WindowShell? _shell;
	private DispatcherQueue? _dispatcherQueue;

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
		_dispatcherQueue = shell.DispatcherQueue;
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

	public DispatcherQueue DispatcherQueue
	{
		get
		{
			EnsureInitialized();
			return _dispatcherQueue;
		}
	}

	[MemberNotNull(nameof(_shell))]
	[MemberNotNull(nameof(_dispatcherQueue))]
	private void EnsureInitialized()
	{
		if (_shell is null || _dispatcherQueue is null)
		{
			throw new InvalidOperationException("WindowShellProvider was not initialized.");
		}
	}
}
