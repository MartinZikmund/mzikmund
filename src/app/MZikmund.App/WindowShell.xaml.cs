using Microsoft.UI.Xaml.Navigation;
using MZikmund.App.Core.Infrastructure;
using MZikmund.Services.Dialogs;
using MZikmund.Services.Navigation;
using MZikmund.ViewModels;
using MZikmund.ViewModels.Admin;
using MUXC = Microsoft.UI.Xaml.Controls;

namespace MZikmund.App;

public sealed partial class WindowShell : Page, IWindowShell
{
	// Views reached from a menu item but not listed in the menu keep their parent highlighted.
	private static readonly Dictionary<string, string> NestedViewTags = new()
	{
		["PostView"] = "Blog",
		["PostEditorView"] = "PostsManager",
	};

	private readonly IServiceScope _windowScope;
	private readonly Window _associatedWindow;
	private readonly INavigationService _navigationService;

	public WindowShell(IServiceProvider serviceProvider, Window associatedWindow)
	{
		InitializeComponent();

		_windowScope = serviceProvider.CreateScope();
		_associatedWindow = associatedWindow;

		var windowShellProvider = (WindowShellProvider)ServiceProvider.GetRequiredService<IWindowShellProvider>();
		windowShellProvider.SetShell(this, associatedWindow);

		_navigationService = ServiceProvider.GetRequiredService<INavigationService>();
		_navigationService.RegisterViewsFromAssembly(typeof(MZikmundApp).Assembly);
		ServiceProvider.GetRequiredService<IDialogService>().RegisterDialogsFromAssembly(typeof(MZikmundApp).Assembly);

		ViewModel = ServiceProvider.GetRequiredService<WindowShellViewModel>();

		CustomizeWindow();

#if HAS_UNO
		// Hardware and browser back; WinAppSDK has no SystemNavigationManager for desktop windows.
		_navigationService.Initialize();
#endif

		_navigationService.Navigate<HomeViewModel>();

		// The first navigation happens before NavigationView is loaded and can show its indicator.
		AppNavigation.Loaded += (_, _) => SyncSelection(InnerFrame.CurrentSourcePageType);
	}

	public IServiceProvider ServiceProvider => _windowScope.ServiceProvider;

	public WindowShellViewModel ViewModel { get; }

	public Frame RootFrame => InnerFrame;

	private void CustomizeWindow()
	{
		_associatedWindow.AppWindow.Title = ViewModel.Title;

		// Mobile and browser heads have no caption area to extend into.
		if (!OperatingSystem.IsAndroid() && !OperatingSystem.IsIOS() && !OperatingSystem.IsBrowser())
		{
			_associatedWindow.ExtendsContentIntoTitleBar = true;
			_associatedWindow.SetTitleBar(TitleBarGrid);
			TitleBarGrid.Visibility = Visibility.Visible;
		}

#if WINDOWS
		_associatedWindow.SystemBackdrop = new MicaBackdrop();
		// Let Mica show through the caption and navigation band; pages keep their own opaque ground.
		ShellRoot.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
#endif
	}

	private async void AppNavigation_ItemInvoked(MUXC.NavigationView sender, MUXC.NavigationViewItemInvokedEventArgs args)
	{
		if (args.IsSettingsInvoked)
		{
			NavigateTopLevel<SettingsViewModel>();
			return;
		}

		if (args.InvokedItemContainer == NavAdminItem)
		{
			ViewModel.RefreshCommentsBadge();
		}

		switch ((args.InvokedItemContainer as FrameworkElement)?.Tag as string)
		{
			case "Home":
				NavigateTopLevel<HomeViewModel>();
				break;
			case "Blog":
				NavigateTopLevel<BlogViewModel>();
				break;
			case "Videos":
				NavigateTopLevel<VideosViewModel>();
				break;
			case "Contact":
				NavigateTopLevel<ContactViewModel>();
				break;
			case "PostsManager":
				NavigateTopLevel<PostsManagerViewModel>();
				break;
			case "MediaBrowser":
				NavigateTopLevel<MediaBrowserViewModel>();
				break;
			case "TagsManager":
				NavigateTopLevel<TagsManagerViewModel>();
				break;
			case "CategoriesManager":
				NavigateTopLevel<CategoriesManagerViewModel>();
				break;
			case "CommentsModerator":
				NavigateTopLevel<CommentsModeratorViewModel>();
				break;
			case "Login":
				await ViewModel.LoginCommand.ExecuteAsync(null);
				break;
			case "Logout":
				await ViewModel.LogoutCommand.ExecuteAsync(null);
				break;
		}
	}

	// Menu destinations are peers, so they start a fresh history instead of stacking on each other.
	private void NavigateTopLevel<TViewModel>()
	{
		_navigationService.Navigate<TViewModel>();
		_navigationService.ClearBackStack();
		ViewModel.CanGoBack = InnerFrame.CanGoBack;
	}

	private void AppNavigation_BackRequested(MUXC.NavigationView sender, MUXC.NavigationViewBackRequestedEventArgs args) =>
		_navigationService.GoBack();

	private void InnerFrame_Navigated(object sender, NavigationEventArgs e)
	{
		ViewModel.CanGoBack = InnerFrame.CanGoBack;
		SyncSelection(e.SourcePageType);
	}

	private void SyncSelection(Type? pageType)
	{
		if (pageType is null)
		{
			return;
		}

		var viewName = pageType.Name;
		if (viewName == "SettingsView")
		{
			AppNavigation.SelectedItem = AppNavigation.SettingsItem;
			return;
		}

		var tag = NestedViewTags.TryGetValue(viewName, out var parentTag) ?
			parentTag :
			viewName.EndsWith("View", StringComparison.Ordinal) ? viewName[..^"View".Length] : viewName;

		AppNavigation.SelectedItem = FindItem(AppNavigation.MenuItems, tag);
	}

	private static MUXC.NavigationViewItem? FindItem(IList<object> items, string tag)
	{
		foreach (var item in items.OfType<MUXC.NavigationViewItem>())
		{
			if (item.Tag as string == tag)
			{
				return item;
			}

			if (FindItem(item.MenuItems, tag) is { } nested)
			{
				return nested;
			}
		}

		return null;
	}
}
