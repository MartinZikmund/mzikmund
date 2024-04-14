namespace MZikmund.Wasm;

public class Program
{
	private static MZikmundApp? _app;

	public static int Main(string[] args)
	{
		Microsoft.UI.Xaml.Application.Start(_ => _app = new AppHead());

		return 0;
	}
}
