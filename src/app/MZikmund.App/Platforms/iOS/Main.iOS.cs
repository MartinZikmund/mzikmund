using Uno.UI.Hosting;

MZikmundApp.InitializeLogging();

var host = UnoPlatformHostBuilder.Create()
	.App(() => new MZikmundApp())
	.UseAppleUIKit()
	.Build();

host.Run();
