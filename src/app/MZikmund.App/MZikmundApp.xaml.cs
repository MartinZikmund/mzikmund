using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MZikmund.Api.Client;
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
using Uno.Extensions.Configuration;
using Uno.Resizetizer;

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

	protected override async void OnLaunched(LaunchActivatedEventArgs args)
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
				)
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
			if (!userService.IsLoggedIn || userService.NeedsRefresh)
			{
				await userService.AuthenticateAsync();
			}
			return userService.AccessToken!;
		};
	}
}
