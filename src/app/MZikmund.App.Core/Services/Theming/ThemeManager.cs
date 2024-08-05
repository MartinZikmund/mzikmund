using MZikmund.App.Core.Infrastructure;

namespace MZikmund.Services.Theming;

public class ThemeManager : IThemeManager
{
	private readonly IApplication _application;

	public ThemeManager(IApplication application)
	{
		_application = application;
	}

	public void SetTheme(AppTheme theme) =>
		GetRootElement().RequestedTheme = theme switch
		{
			AppTheme.System => ElementTheme.Default,
			AppTheme.Light => ElementTheme.Light,
			AppTheme.Dark => ElementTheme.Dark,
			_ => throw new ArgumentOutOfRangeException(nameof(theme))
		};

	public AppTheme CurrentTheme =>
		GetRootElement().RequestedTheme switch
		{
			ElementTheme.Default => Application.Current.RequestedTheme == ApplicationTheme.Dark ?
				AppTheme.Dark : AppTheme.Light,
			ElementTheme.Light => AppTheme.Light,
			ElementTheme.Dark => AppTheme.Dark,
			_ => throw new ArgumentOutOfRangeException()
		};

	private FrameworkElement GetRootElement()
	{
		var rootElement = _application.MainWindow?.Content as FrameworkElement;
		if (rootElement == null)
		{
			throw new InvalidOperationException("Root element of the window is not a FrameworkElement");
		}

		return rootElement;
	}
}
