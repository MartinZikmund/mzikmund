﻿using Microsoft.UI.Dispatching;
using MZikmund.ViewModels;
using Uno.Disposables;

namespace MZikmund.ViewModels;

public class WindowShellViewModel : ViewModelBase
{
	private readonly DispatcherQueue _dispatcher;
	private RefCountDisposable? _refCountDisposable;

	public WindowShellViewModel(DispatcherQueue dispatcher)
	{
		_dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
	}

	public string Title { get; set; } = "Martin Zikmund";

	public IDisposable BeginLoading()
	{
		LoadingStatusMessage = "";
		if (_refCountDisposable != null && !_refCountDisposable.IsDisposed)
		{
			return _refCountDisposable.GetDisposable();
		}

		IsLoading = true;
		_refCountDisposable = new RefCountDisposable(Disposable.Create(
			() => // TODO: Await TryEneque
			{
#if __WASM__
				IsLoading = false;
				return;
#else
				if (_dispatcher.HasThreadAccess)
				{
					IsLoading = false;
				}
				else
				{
					_dispatcher.TryEnqueue(DispatcherQueuePriority.Normal, () =>
					{
						if (_refCountDisposable == null || _refCountDisposable.IsDisposed)
						{
							IsLoading = false;
						}
					});
				}
#endif
			}));
		return _refCountDisposable;
	}

	public bool IsLoading { get; private set; }

	public string LoadingStatusMessage { get; set; } = "";

	public void BackRequested()
	{
		//NavigationService.GoBack();
	}
}
