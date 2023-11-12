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

namespace MZikmund;

public sealed partial class AppShell : Page
{
	private readonly UISettings _uiSettings = new UISettings();
	private static AppShell _instance;

	private AppShell()
	{
		InitializeComponent();
		ViewModel = new AppShellViewModel(DispatcherQueue);
		_uiSettings.ColorValuesChanged += ColorValuesChanged;
		SetupCoreWindow();

		Loaded += AppShell_Loaded;
	}

	private void AppShell_Loaded(object sender, RoutedEventArgs e)
	{
		SetTitlebarColors();

#if WINDOWS_UWP
		BackdropMaterial.SetApplyToRootOrPageBackground(this, true);
#endif
	}

	public AppShellViewModel ViewModel { get; }

	public Frame RootFrame => InnerFrame;

	public static AppShell GetForCurrentView() => _instance = new AppShell(); // TODO: Make instance window-specific

	private void SetupCoreWindow()
	{
		// TODO: Adjust extend into titlebar
//#pragma warning disable CS8618 
//		var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
//		coreTitleBar.ExtendViewIntoTitleBar = true;
//		coreTitleBar.LayoutMetricsChanged += CoreTitleBar_LayoutMetricsChanged;
//#pragma warning restore CS8618
	}

	//private void CoreTitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
	//{
	//	TitleBarGrid.Height = sender.Height;
	//}

	private async void ColorValuesChanged(UISettings sender, object args)
	{
		await Dispatcher.RunAsync(
			CoreDispatcherPriority.Normal,
			SetTitlebarColors);
	}

	private void SetTitlebarColors()
	{
#pragma warning disable CS8618
#pragma warning disable Uno0001
		var brandColor = ColorResources.BrandColor;
		var titleBar = ApplicationView.GetForCurrentView().TitleBar;
		titleBar.BackgroundColor = brandColor;
		titleBar.ButtonBackgroundColor = Colors.Transparent;
		titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
		if (Ioc.Default.GetRequiredService<IThemeManager>().CurrentTheme == AppTheme.Dark)
		{
			titleBar.ButtonForegroundColor = Colors.White;
			titleBar.ButtonInactiveForegroundColor = Colors.Gray;
			titleBar.ButtonHoverBackgroundColor = Color.FromArgb(100, 100, 100, 100);
			titleBar.ButtonHoverForegroundColor = Colors.White;
			titleBar.ButtonPressedBackgroundColor = Color.FromArgb(200, 100, 100, 100);
			titleBar.ButtonPressedForegroundColor = Colors.White;
		}
		else
		{
			titleBar.ButtonForegroundColor = Colors.Black;
			titleBar.ButtonInactiveForegroundColor = Colors.Gray;
			titleBar.ButtonHoverBackgroundColor = Color.FromArgb(100, 200, 200, 200);
			titleBar.ButtonHoverForegroundColor = Colors.Black;
			titleBar.ButtonPressedBackgroundColor = Color.FromArgb(200, 200, 200, 200);
			titleBar.ButtonPressedForegroundColor = Colors.Black;
		}
#pragma warning restore Uno0001
#pragma warning restore CS8618
	}

	private void MenuItemInvoked(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args)
	{
		var navigationService = Ioc.Default.GetRequiredService<INavigationService>();
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
		else if (args.InvokedItemContainer == AdminTagsNavigationViewItem)
		{
			navigationService.Navigate<BlogTagsManagerViewModel>();
		}
		else if (args.InvokedItemContainer == AdminCategoriesNavigationViewItem)
		{
			navigationService.Navigate<BlogCategoriesManagerViewModel>();
		}
	}
}
