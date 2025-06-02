using Android.Runtime;

namespace MZikmund.App.Droid;
[global::Android.App.ApplicationAttribute(
	Label = "@string/ApplicationName",
	Icon = "@mipmap/icon",
	LargeHeap = true,
	HardwareAccelerated = true,
	Theme = "@style/AppTheme"
)]
public class Application : Microsoft.UI.Xaml.NativeApplication
{
	static Application()
	{
		MZikmundApp.InitializeLogging();
	}

	public Application(IntPtr javaReference, JniHandleOwnership transfer)
		: base(() => new MZikmundApp(), javaReference, transfer)
	{
	}
}

