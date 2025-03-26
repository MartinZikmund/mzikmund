using Uno.UI.Runtime.Skia;

namespace MZikmund.App;

public class Program
{
	[STAThread]
	public static void Main(string[] args)
	{
		MZikmundApp.InitializeLogging();

		var host = SkiaHostBuilder.Create()
			.App(() => new MZikmundApp())
			.UseX11()
			.UseLinuxFrameBuffer()
			.UseMacOS()
			.UseWindows()
			.Build();

		host.Run();
	}
}
