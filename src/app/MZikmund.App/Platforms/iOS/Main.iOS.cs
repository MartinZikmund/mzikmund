using Uno.UI.Hosting;

var host = UnoPlatformHostBuilder.Create()
	.App(() => new MZikmundApp())
	.UseAppleUIKit()
	.Build();

host.Run();
