// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppLifecycle;
using Windows.ApplicationModel.Activation;

namespace MZikmund.App;

public static class MZikmundProgram
{
	private static DispatcherQueue _dispatcherQueue;

	[STAThread]
	static void Main(string[] args)
	{
		WinRT.ComWrappersSupport.InitializeComWrappers();
		Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().Activated += OnActivated;

		bool isRedirect = DecideRedirection().GetAwaiter().GetResult();

		if (!isRedirect)
		{
			Microsoft.UI.Xaml.Application.Start((p) =>
			{
				_dispatcherQueue = DispatcherQueue.GetForCurrentThread();
				var context = new DispatcherQueueSynchronizationContext(_dispatcherQueue);
				SynchronizationContext.SetSynchronizationContext(context);
				new MZikmundApp();
			});
		}
	}

	private static async Task<bool> DecideRedirection()
	{
		var mainInstance = Microsoft.Windows.AppLifecycle.AppInstance.FindOrRegisterForKey("main");
		var activatedEventArgs = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs();

		if (!mainInstance.IsCurrent)
		{
			// Redirect the activation (and args) to the "main" instance, and exit.
			await mainInstance.RedirectActivationToAsync(activatedEventArgs);
			return true;
		}

		return false;
	}

	// Handle Redirection
	public static void OnActivated(object sender, AppActivationArguments args)
	{
		if (args.Kind == ExtendedActivationKind.Protocol)
		{
			var protocolArgs = (ProtocolActivatedEventArgs)args.Data;
			_dispatcherQueue.TryEnqueue(() =>
			{
				if (protocolArgs.Uri.Authority == "oauthcallback")
				{
					((MZikmundApp)MZikmundApp.Current).OnUriCallback(protocolArgs.Uri);
				}
				var window = ((MZikmundApp)MZikmundApp.Current).MainWindow;
				var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(window);
				SetForegroundWindow(windowHandle);
			});

		}
	}

	// P/Invoke declaration for SetForegroundWindow
	[DllImport("user32.dll")]
	private static extern bool SetForegroundWindow(IntPtr hWnd);
}
