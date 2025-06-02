using Uno.UI.Hosting;

MZikmundApp.InitializeLogging();

var host = UnoPlatformHostBuilder.Create()
	.App(() => new MZikmundApp())
	.UseWebAssembly()
	.Build();

await host.RunAsync();
