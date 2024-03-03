using Microsoft.UI;
using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using MZikmund.Resources;
using CommunityToolkit.Mvvm.DependencyInjection;
using MZikmund.Services.Theming;
using MZikmund.ViewModels;
using MZikmund.ViewModels.Admin;
using MZikmund.Services.Navigation;
using MZikmund.Services.Dialogs;
using Windows.Foundation.Metadata;

namespace MZikmund;

public sealed partial class WindowShell : Page
{
	private readonly UISettings _uiSettings = new UISettings();
	private readonly IServiceScope _windowScope;
	private readonly Window _associatedWindow;

	public WindowShell(IServiceProvider serviceProvider, Window associatedWindow)
	{
		InitializeComponent();
		ViewModel = new WindowShellViewModel(DispatcherQueue);

		_windowScope = serviceProvider.CreateScope();
		var windowShellProvider = (WindowShellProvider)ServiceProvider.GetRequiredService<IWindowShellProvider>();
		windowShellProvider.SetShell(this);
		ServiceProvider.GetRequiredService<INavigationService>().RegisterViewsFromAssembly(typeof(App).Assembly);
		ServiceProvider.GetRequiredService<IDialogService>().RegisterDialogsFromAssembly(typeof(App).Assembly);

		_uiSettings.ColorValuesChanged += ColorValuesChanged;
		_associatedWindow = associatedWindow;
		CustomizeWindow();

		Loaded += WindowShell_Loaded;
	}

	public IServiceProvider ServiceProvider => _windowScope.ServiceProvider;

	private void WindowShell_Loaded(object sender, RoutedEventArgs e)
	{
		SetTitlebarColors();

#if WINDOWS_UWP
		BackdropMaterial.SetApplyToRootOrPageBackground(this, true);
#endif
	}

	public WindowShellViewModel ViewModel { get; }

	public Frame RootFrame => InnerFrame;

	public bool HasCustomTitleBar { get; private set; }

	private void CustomizeWindow()
	{
		if (ApiInformation.IsPropertyPresent("Microsoft.UI.Xaml.Window", "ExtendsContentIntoTitleBar"))
		{
			_associatedWindow.ExtendsContentIntoTitleBar = true;
			_associatedWindow.SetTitleBar(TitleBarGrid);
			HasCustomTitleBar = true;
		}
		_associatedWindow.AppWindow.Title = "Martin Zikmund";
#if !HAS_UNO_WINUI
		_associatedWindow.SystemBackdrop = new MicaBackdrop();
#endif
	}

	private async void ColorValuesChanged(UISettings sender, object args)
	{
		await Dispatcher.RunAsync(
			CoreDispatcherPriority.Normal,
			SetTitlebarColors);
	}

	private void SetTitlebarColors()
	{

		//TODO:Titlebar colors
		//#pragma warning disable CS8618
		//#pragma warning disable Uno0001
		//		var brandColor = ColorResources.BrandColor;
		//		var titleBar = ApplicationView.GetForCurrentView().TitleBar;
		//		titleBar.BackgroundColor = brandColor;
		//		titleBar.ButtonBackgroundColor = Colors.Transparent;
		//		titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
		//		if (Ioc.Default.GetRequiredService<IThemeManager>().CurrentTheme == AppTheme.Dark)
		//		{
		//			titleBar.ButtonForegroundColor = Colors.White;
		//			titleBar.ButtonInactiveForegroundColor = Colors.Gray;
		//			titleBar.ButtonHoverBackgroundColor = Color.FromArgb(100, 100, 100, 100);
		//			titleBar.ButtonHoverForegroundColor = Colors.White;
		//			titleBar.ButtonPressedBackgroundColor = Color.FromArgb(200, 100, 100, 100);
		//			titleBar.ButtonPressedForegroundColor = Colors.White;
		//		}
		//		else
		//		{
		//			titleBar.ButtonForegroundColor = Colors.Black;
		//			titleBar.ButtonInactiveForegroundColor = Colors.Gray;
		//			titleBar.ButtonHoverBackgroundColor = Color.FromArgb(100, 200, 200, 200);
		//			titleBar.ButtonHoverForegroundColor = Colors.Black;
		//			titleBar.ButtonPressedBackgroundColor = Color.FromArgb(200, 200, 200, 200);
		//			titleBar.ButtonPressedForegroundColor = Colors.Black;
		//		}
		//#pragma warning restore Uno0001
		//#pragma warning restore CS8618
	}

	private void MenuItemInvoked(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args)
	{
		var navigationService = ServiceProvider.GetRequiredService<INavigationService>();
		if (args.IsSettingsInvoked)
		{
			navigationService.Navigate<SettingsViewModel>();
		}

		if (args.InvokedItemContainer == HomeNavigationViewItem)
		{

		}
		else if (args.InvokedItemContainer == BlogNavigationViewItem)
		{

		}
		else if (args.InvokedItemContainer == ContactNavigationViewItem)
		{

		}
		else if (args.InvokedItemContainer == AdminPostsNavigationViewItem)
		{
			navigationService.Navigate<PostsManagerViewModel>();
		}
		else if (args.InvokedItemContainer == AdminTagsNavigationViewItem)
		{
			navigationService.Navigate<TagsManagerViewModel>();
		}
		else if (args.InvokedItemContainer == AdminCategoriesNavigationViewItem)
		{
			navigationService.Navigate<CategoriesManagerViewModel>();
		}
	}
}
