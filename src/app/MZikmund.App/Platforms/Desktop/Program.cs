using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia;

namespace MZikmund.App;

public class Program
{
	[STAThread]
	public static void Main(string[] args)
	{
		MZikmundApp.InitializeLogging();

		var host = UnoPlatformHostBuilder.Create()
			.App(() => new MZikmundApp())
			.UseX11()
			.UseLinuxFrameBuffer()
			.UseMacOS()
			.UseWin32()
			.Build();

		host.Run();
	}
}
