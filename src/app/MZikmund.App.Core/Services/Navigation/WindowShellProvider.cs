
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Dispatching;
using MZikmund.ViewModels;
using MZikmund.App.Core.Infrastructure;

namespace MZikmund.Services.Navigation;

public sealed class WindowShellProvider : IWindowShellProvider
{
	private IWindowShell? _shell;
	private Window? _window;
	private DispatcherQueue? _dispatcherQueue;

	public WindowShellProvider()
	{
	}

	public void SetShell(IWindowShell shell, Window window)
	{
		if (shell is null)
		{
			throw new ArgumentNullException(nameof(shell));
		}

		_shell = shell;
		_window = window;
		_dispatcherQueue = shell.DispatcherQueue;
	}

	public Window Window
	{
		get
		{
			EnsureInitialized();
			return _window;
		}
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

	public IWindowShell WindowShell
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

	public Frame RootFrame
	{
		get
		{
			EnsureInitialized();
			return _shell.RootFrame;
		}
	}

	[MemberNotNull(nameof(_shell))]
	[MemberNotNull(nameof(_window))]
	[MemberNotNull(nameof(_dispatcherQueue))]
	private void EnsureInitialized()
	{
		if (_shell is null || _dispatcherQueue is null || _window is null)
		{
			throw new InvalidOperationException("WindowShellProvider was not initialized.");
		}
	}
}
