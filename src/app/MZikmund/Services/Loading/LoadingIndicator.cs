namespace MZikmund.Services.Loading;

public class LoadingIndicator : ILoadingIndicator
{
	public IDisposable BeginLoading() => AppShell.GetForCurrentView().ViewModel.BeginLoading();

	public bool IsLoading => AppShell.GetForCurrentView().ViewModel.IsLoading;

	public string StatusMessage
	{
		get => AppShell.GetForCurrentView().ViewModel.LoadingStatusMessage;
		set => AppShell.GetForCurrentView().ViewModel.LoadingStatusMessage = value;
	}
}
