using System.Diagnostics;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Options;
using MZikmund.Api.Client;
using MZikmund.Api.Handlers;
using MZikmund.App.Core.Infrastructure;
using MZikmund.Business.Models;
using MZikmund.DataContracts.Serialization;
using MZikmund.Infrastructure;
using MZikmund.Services.Account;
using MZikmund.Services.Caching;
using MZikmund.Services.Dialogs;
using MZikmund.Services.Endpoints;
using MZikmund.Services.Loading;
using MZikmund.Services.Navigation;
using MZikmund.Services.Preferences;
using MZikmund.Services.Theming;
using MZikmund.Services.Timers;
using MZikmund.ViewModels;
using MZikmund.ViewModels.Admin;
using MZikmund.Web.Core.Services;
using Refit;
using Uno.Extensions;

namespace MZikmund.App;

public partial class MZikmundApp : Application, IApplication
{
	/// <summary>
	/// Initializes the singleton application object. This is the first line of authored code
	/// executed, and as such is the logical equivalent of main() or WinMain().
	/// </summary>
	public MZikmundApp()
	{
		this.InitializeComponent();
	}

	public Window? MainWindow { get; private set; }

	internal static IHost? Host { get; private set; }

	protected override void OnLaunched(LaunchActivatedEventArgs args)
	{
		var builder = this.CreateBuilder(args)
			.Configure(host => host
#if DEBUG
				// Switch to Development environment when running in DEBUG
				.UseEnvironment(Environments.Development)
#endif
				.UseConfiguration(configure: configBuilder =>
					configBuilder
						.EmbeddedSource<MZikmundApp>()
						.Section<AppConfig>()
						.Section<AuthConfig>()
				)
				.UseLocalization()
				.UseHttp((context, services) => services
#if DEBUG
				// DelegatingHandler will be automatically injected into Refit Client
				.AddTransient<DelegatingHandler, DebugHttpHandler>()
#endif
				.AddRefitClient<IApiClient>(context))
				.UseDefaultServiceProvider((context, options) =>
				{
					options.ValidateScopes = true;
					options.ValidateOnBuild = true;
				})
				.ConfigureServices((context, services) => ConfigureServices(services))
			);
		MainWindow = builder.Window;

#if DEBUG
		MainWindow.UseStudio();
#endif

		Host = builder.Build();
		Ioc.Default.ConfigureServices(Host.Services);

		// Do not repeat app initialization when the Window already has content,
		// just ensure that the window is active
		if (MainWindow.Content is not WindowShell windowShell)
		{
			// Create a Frame to act as the navigation context and navigate to the first page
			windowShell = new WindowShell(Host.Services, MainWindow);

			// Place the frame in the current Window
			MainWindow.Content = windowShell;
		}

		// Ensure the current window is active
		MainWindow.Activate();
	}

	private void ConfigureServices(IServiceCollection services)
	{
		services.AddScoped<BlogViewModel>();
		services.AddScoped<SettingsViewModel>();
		services.AddScoped<TagsManagerViewModel>();
		services.AddScoped<CategoriesManagerViewModel>();
		services.AddScoped<AddOrUpdateCategoryDialogViewModel>();
		services.AddScoped<AddOrUpdateTagDialogViewModel>();
		services.AddScoped<PostsManagerViewModel>();
		services.AddScoped<PostEditorViewModel>();
		services.AddScoped<PostViewModel>();
		services.AddScoped<WindowShellViewModel>();

		services.AddSingleton<IApplication>(this);
		services.AddSingleton<IThemeManager, ThemeManager>();
		services.AddSingleton<IAppPreferences, AppPreferences>();
		services.AddSingleton<IPreferencesService, PreferencesService>();
		services.AddScoped<IDialogCoordinator, DialogCoordinator>();
		services.AddScoped<IFrameProvider, FrameProvider>();
		services.AddScoped<INavigationService, NavigationService>();
		services.AddScoped<ILoadingIndicator, LoadingIndicator>();
		services.AddScoped<IDialogService, DialogService>();
		services.AddScoped<IWindowShellProvider, WindowShellProvider>();
		services.AddScoped<ITimerFactory, TimerFactory>();
		services.AddSingleton<IUserService, UserService>();
		services.AddSingleton<IMarkdownConverter, MarkdownConverter>();
		services.AddSingleton<IPostContentProcessor, PostContentProcessor>();

		services.AddSingleton(CreateApi);
	}

	private static IMZikmundApi CreateApi(IServiceProvider provider)
	{
		var configuration = provider.GetRequiredService<IOptions<AppConfig>>();

		if (configuration.Value.ApiUrl is not { } apiUrl)
		{
			throw new InvalidOperationException("API URL is not set in configuration");
		}

		return RestService.For<IMZikmundApi>(apiUrl, new RefitSettings()
		{
			AuthorizationHeaderValueGetter = GetTokenAsync
		});

		async Task<string> GetTokenAsync(HttpRequestMessage message, CancellationToken cancellationToken)
		{
			var userService = Ioc.Default.GetRequiredService<IUserService>();
			if (!userService.IsLoggedIn || userService.NeedsRefresh)
			{
				await userService.AuthenticateAsync();
			}

			if (userService.AccessToken is not { } accessToken)
			{
				throw new InvalidOperationException("Access token is not set");
			}

			return accessToken;
		}
	}

	/// <summary>
	/// Configures global Uno Platform logging
	/// </summary>
	public static void InitializeLogging()
	{
#if DEBUG
		// Logging is disabled by default for release builds, as it incurs a significant
		// initialization cost from Microsoft.Extensions.Logging setup. If startup performance
		// is a concern for your application, keep this disabled. If you're running on the web or
		// desktop targets, you can use URL or command line parameters to enable it.
		//
		// For more performance documentation: https://platform.uno/docs/articles/Uno-UI-Performance.html

		var factory = LoggerFactory.Create(builder =>
		{
#if __WASM__
			builder.AddProvider(new global::Uno.Extensions.Logging.WebAssembly.WebAssemblyConsoleLoggerProvider());
#elif __IOS__ || __MACCATALYST__
			builder.AddProvider(new global::Uno.Extensions.Logging.OSLogLoggerProvider());
#else
			builder.AddConsole();
#endif

			// Exclude logs below this level
			builder.SetMinimumLevel(LogLevel.Information);

			// Default filters for Uno Platform namespaces
			builder.AddFilter("Uno", LogLevel.Warning);
			builder.AddFilter("Windows", LogLevel.Warning);
			builder.AddFilter("Microsoft", LogLevel.Warning);

			// Generic Xaml events
			// builder.AddFilter("Microsoft.UI.Xaml", LogLevel.Debug );
			// builder.AddFilter("Microsoft.UI.Xaml.VisualStateGroup", LogLevel.Debug );
			// builder.AddFilter("Microsoft.UI.Xaml.StateTriggerBase", LogLevel.Debug );
			// builder.AddFilter("Microsoft.UI.Xaml.UIElement", LogLevel.Debug );
			// builder.AddFilter("Microsoft.UI.Xaml.FrameworkElement", LogLevel.Trace );

			// Layouter specific messages
			// builder.AddFilter("Microsoft.UI.Xaml.Controls", LogLevel.Debug );
			// builder.AddFilter("Microsoft.UI.Xaml.Controls.Layouter", LogLevel.Debug );
			// builder.AddFilter("Microsoft.UI.Xaml.Controls.Panel", LogLevel.Debug );

			// builder.AddFilter("Windows.Storage", LogLevel.Debug );

			// Binding related messages
			// builder.AddFilter("Microsoft.UI.Xaml.Data", LogLevel.Debug );
			// builder.AddFilter("Microsoft.UI.Xaml.Data", LogLevel.Debug );

			// Binder memory references tracking
			// builder.AddFilter("Uno.UI.DataBinding.BinderReferenceHolder", LogLevel.Debug );

			// DevServer and HotReload related
			// builder.AddFilter("Uno.UI.RemoteControl", LogLevel.Information);

			// Debug JS interop
			// builder.AddFilter("Uno.Foundation.WebAssemblyRuntime", LogLevel.Debug );
		});

		global::Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = factory;

#if HAS_UNO
		global::Uno.UI.Adapter.Microsoft.Extensions.Logging.LoggingAdapter.Initialize();
#endif
#endif
	}

#if WINDOWS
	internal void OnUriCallback(Uri uri)
	{
		if (!Microsoft.Security.Authentication.OAuth.OAuth2Manager.CompleteAuthRequest(uri))
		{
			this.Log().LogError("Could not authenticate");
		}
	}
#endif
}
