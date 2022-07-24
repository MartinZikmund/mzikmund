using Tizen.Applications;
using Uno.UI.Runtime.Skia;

namespace MZikmund.App.Skia.Tizen
{
	class Program
{
	static void Main(string[] args)
	{
		var host = new TizenHost(() => new MZikmund.App.App());
		host.Run();
	}
}
}
