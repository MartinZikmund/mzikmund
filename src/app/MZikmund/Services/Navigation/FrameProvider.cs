namespace MZikmund.Services.Navigation;

public class FrameProvider : IFrameProvider
{
	public Frame GetForCurrentView() => AppShell.GetForCurrentView().RootFrame;
}
