using Windows.ApplicationModel;
using MZikmund.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace MZikmund.Views;

public abstract partial class PageBase<TViewModel> : Page
	where TViewModel : PageViewModel
{
	protected PageBase()
	{
		Loading += PageLoading;
		Loaded += PageLoaded;
		Unloaded += PageUnloaded;
	}

	public virtual TViewModel? ViewModel { get; private set; }

	private void PageLoading(object sender, object args)
	{
		if (XamlRoot?.Content is WindowShell windowShell)
		{
			ViewModel = windowShell.ServiceProvider.GetRequiredService<TViewModel>();
		}
		ViewModel?.ViewAppearing();
	}

	private void PageLoaded(object sender, RoutedEventArgs e)
	{
		ViewModel?.ViewAppeared();
	}

	private void PageUnloaded(object sender, RoutedEventArgs e)
	{
		ViewModel?.ViewDestroy();
	}
}
