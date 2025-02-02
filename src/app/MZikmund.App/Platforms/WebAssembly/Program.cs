namespace MZikmund.App;

public class Program
{
	private static MZikmundApp? _app;

	public static int Main(string[] args)
	{
        MZikmundApp.InitializeLogging();
		
		Microsoft.UI.Xaml.Application.Start(_ => _app = new MZikmundApp());

		return 0;
	}
}
