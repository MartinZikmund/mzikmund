using Microsoft.UI.Xaml.Navigation;
using MZikmund.App.Core.Infrastructure;
using MZikmund.ViewModels;
using Windows.ApplicationModel;

namespace MZikmund.Views;

public abstract partial class PageBase<TViewModel> : Page
	where TViewModel : PageViewModel
{
	private object? _pendingParameter;
	private bool _isNavigationDelayed;

	protected PageBase()
	{
		Loading += PageLoading;
		Loaded += PageLoaded;
		Unloaded += PageUnloaded;
	}

	public virtual TViewModel? ViewModel { get; private set; }

	private void PageLoading(object sender, object args)
	{
		EnsureViewModel();

		if (_isNavigationDelayed && ViewModel is not null)
		{
			ViewModel.ViewNavigatedTo(_pendingParameter);
			_pendingParameter = null;
			_isNavigationDelayed = false;
		}

		ViewModel?.ViewLoading();
	}

	private void PageLoaded(object sender, RoutedEventArgs e)
	{
		ViewModel?.ViewLoaded();
	}

	protected override void OnNavigatedTo(NavigationEventArgs e)
	{
		EnsureViewModel();

		if (ViewModel is not null)
		{
			ViewModel.ViewNavigatedTo(_pendingParameter ?? e.Parameter);
		}
		else
		{
			_isNavigationDelayed = true;
			_pendingParameter = e.Parameter;
		}
	}

	private void PageUnloaded(object sender, RoutedEventArgs e)
	{
		ViewModel?.ViewUnloaded();
	}

	private void EnsureViewModel()
	{
		if (ViewModel is not null)
		{
			return;
		}

		if (XamlRoot?.Content is IWindowShell windowShell)
		{
			ViewModel = windowShell.ServiceProvider.GetRequiredService<TViewModel>();
		}
	}
}
