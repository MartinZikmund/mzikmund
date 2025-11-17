using Uno.UI.Hosting;

var host = UnoPlatformHostBuilder.Create()
	.App(() => new MZikmundApp())
	.UseWebAssembly()
	.Build();

await host.RunAsync();
