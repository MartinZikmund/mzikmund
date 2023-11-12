namespace MZikmund.Services.Loading;

public class LoadingIndicator : ILoadingIndicator
{
	public IDisposable BeginLoading() => WindowShell.GetForCurrentView().ViewModel.BeginLoading();

	public bool IsLoading => WindowShell.GetForCurrentView().ViewModel.IsLoading;

	public string StatusMessage
	{
		get => WindowShell.GetForCurrentView().ViewModel.LoadingStatusMessage;
		set => WindowShell.GetForCurrentView().ViewModel.LoadingStatusMessage = value;
	}
}
