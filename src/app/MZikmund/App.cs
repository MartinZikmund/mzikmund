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
				.UseAuthentication(auth =>
	auth.AddMsal(name: "MsalAuthentication")
				)
				.ConfigureServices((context, services) =>
				{
					// TODO: Register your services
					//services.AddSingleton<IMyService, MyService>();
				})
			);
		MainWindow = builder.Window;

		Host = builder.Build();

		// Do not repeat app initialization when the Window already has content,
		// just ensure that the window is active
		if (MainWindow.Content is not Frame rootFrame)
		{
			// Create a Frame to act as the navigation context and navigate to the first page
			rootFrame = new Frame();

			// Place the frame in the current Window
			MainWindow.Content = rootFrame;
		}

		if (rootFrame.Content == null)
		{
			// When the navigation stack isn't restored navigate to the first page,
			// configuring the new page by passing required information as a navigation
			// parameter
			rootFrame.Navigate(typeof(MainPage), args.Arguments);
		}
		// Ensure the current window is active
		MainWindow.Activate();
	}
}
