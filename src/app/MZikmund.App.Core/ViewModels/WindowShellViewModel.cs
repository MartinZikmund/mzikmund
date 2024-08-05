using Microsoft.UI.Dispatching;
using MZikmund.Services.Navigation;
using Uno.Disposables;

namespace MZikmund.ViewModels;

public partial class WindowShellViewModel : ViewModelBase
{
	private readonly IWindowShellProvider _provider;
	private readonly INavigationService _navigationService;
	private RefCountDisposable? _refCountDisposable;

	[ObservableProperty]
	private bool _isLoading;

	[ObservableProperty]
	private string _loadingStatusMessage = "";

	public WindowShellViewModel(IWindowShellProvider provider, INavigationService navigationService)
	{
		_provider = provider ?? throw new ArgumentNullException(nameof(provider));
		_navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
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
			() => // TODO: Await TryEnequeAsync
			{
#if __WASM__
				IsLoading = false;
				return;
#else
				if (_provider.DispatcherQueue.HasThreadAccess)
				{
					IsLoading = false;
				}
				else
				{
					_provider.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
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


	public void BackRequested() => _navigationService.GoBack();
}
