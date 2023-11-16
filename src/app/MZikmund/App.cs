using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Options;
using MZikmund.Api.Client;
using MZikmund.Services.Account;
using MZikmund.Services.Dialogs;
using MZikmund.Services.Loading;
using MZikmund.Services.Navigation;
using MZikmund.Services.Preferences;
using MZikmund.Services.Theming;
using MZikmund.ViewModels;
using MZikmund.ViewModels.Admin;
using Refit;

namespace MZikmund;

public class App : Application
{
	protected Window? MainWindow { get; private set; }

	protected IHost? Host { get; private set; }

	protected override void OnLaunched(LaunchActivatedEventArgs args)
	{
		var builder = this.CreateBuilder(args)
			.Configure(host => host
#if DEBUG
				// Switch to Development environment when running in DEBUG
				.UseEnvironment(Environments.Development)
#endif
				.UseLogging(configure: (context, logBuilder) =>
				{
					// Configure log levels for different categories of logging
					logBuilder
						.SetMinimumLevel(
							context.HostingEnvironment.IsDevelopment() ?
								LogLevel.Information :
								LogLevel.Warning)

						// Default filters for core Uno Platform namespaces
						.CoreLogLevel(LogLevel.Warning);

					// Uno Platform namespace filter groups
					// Uncomment individual methods to see more detailed logging
					//// Generic Xaml events
					//logBuilder.XamlLogLevel(LogLevel.Debug);
					//// Layout specific messages
					//logBuilder.XamlLayoutLogLevel(LogLevel.Debug);
					//// Storage messages
					//logBuilder.StorageLogLevel(LogLevel.Debug);
					//// Binding related messages
					//logBuilder.XamlBindingLogLevel(LogLevel.Debug);
					//// Binder memory references tracking
					//logBuilder.BinderMemoryReferenceLogLevel(LogLevel.Debug);
					//// DevServer and HotReload related
					//logBuilder.HotReloadCoreLogLevel(LogLevel.Information);
					//// Debug JS interop
					//logBuilder.WebAssemblyLogLevel(LogLevel.Debug);

				}, enableUnoLogging: true)
				.UseConfiguration(configure: configBuilder =>
					configBuilder
						.EmbeddedSource<App>()
						.Section<AppConfig>()
				)
				// Enable localization (see appsettings.json for supported languages)
				.UseLocalization()
				// Register Json serializers (ISerializer and ISerializer)
				.UseSerialization((context, services) => services
					.AddContentSerializer(context)
					.AddJsonTypeInfo(WeatherForecastContext.Default.IImmutableListWeatherForecast))
				.UseHttp((context, services) => services
					// Register HttpClient
#if DEBUG
					// DelegatingHandler will be automatically injected into Refit Client
					.AddTransient<DelegatingHandler, DebugHttpHandler>()
#endif
					.AddSingleton<IWeatherCache, WeatherCache>()
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
		MainWindow.EnableHotReload();
#endif

		Host = builder.Build();

		// Do not repeat app initialization when the Window already has content,
		// just ensure that the window is active
		if (MainWindow.Content is not WindowShell windowShell)
		{
			// Create a Frame to act as the navigation context and navigate to the first page
			windowShell = new WindowShell(Host.Services);

			// Place the frame in the current Window
			MainWindow.Content = windowShell;
		}

		//if (windowShell.Content == null)
		//{
		//	// When the navigation stack isn't restored navigate to the first page,
		//	// configuring the new page by passing required information as a navigation
		//	// parameter
		//	windowShell.Navigate(typeof(MainPage), args.Arguments);
		//}
		// Ensure the current window is active
		MainWindow.Activate();
	}

	private static void ConfigureServices(IServiceCollection services)
	{
		services.AddScoped<SettingsViewModel>();
		services.AddScoped<BlogTagsManagerViewModel>();
		services.AddScoped<BlogCategoriesManagerViewModel>();
		services.AddScoped<AddOrUpdateBlogCategoryDialogViewModel>();
		services.AddScoped<AddOrUpdateBlogTagDialogViewModel>();

		services.AddSingleton<IThemeManager, ThemeManager>();
		services.AddSingleton<IAppPreferences, AppPreferences>();
		services.AddSingleton<IPreferencesService, PreferencesService>();
		services.AddScoped<IDialogCoordinator, DialogCoordinator>();
		services.AddScoped<IFrameProvider, FrameProvider>();
		services.AddScoped<INavigationService, NavigationService>();
		services.AddScoped<ILoadingIndicator, LoadingIndicator>();
		services.AddScoped<IDialogService, DialogService>();
		services.AddScoped<IWindowShellProvider, WindowShellProvider>();
		services.AddSingleton(provider =>
		{
			var configuration = provider.GetRequiredService<IOptions<AppConfig>>();
			return RestService.For<IMZikmundApi>(configuration.Value.ApiUrl!, new RefitSettings()
			{
				AuthorizationHeaderValueGetter = GetTokenAsync
			});
		});

		async Task<string> GetTokenAsync(HttpRequestMessage message, CancellationToken cancellationToken)
		{
			//TODO: Move somewhere more appropriate and integrate refresh token support
			var userService = Ioc.Default.GetRequiredService<IUserService>();
			if (!userService.IsLoggedIn)
			{
				await userService.AuthenticateAsync();
			}
			return userService.AccessToken!;
		};
	}
}
