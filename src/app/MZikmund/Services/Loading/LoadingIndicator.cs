using MZikmund.Services.Navigation;

namespace MZikmund.Services.Loading;

public class LoadingIndicator : ILoadingIndicator
{
	private readonly IWindowShellProvider _windowShellProvider;

	public LoadingIndicator(IWindowShellProvider windowShellProvider)
	{
		_windowShellProvider = windowShellProvider;
	}

	public IDisposable BeginLoading() => _windowShellProvider.ViewModel.BeginLoading();

	public bool IsLoading => _windowShellProvider.ViewModel.IsLoading;

	public string StatusMessage
	{
		get => _windowShellProvider.ViewModel.LoadingStatusMessage;
		set => _windowShellProvider.ViewModel.LoadingStatusMessage = value;
	}
}
