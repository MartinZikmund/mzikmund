using Android.Runtime;
using Uno.UI.Hosting;

namespace MZikmund.App.Droid;

[global::Android.App.ApplicationAttribute(
	Label = "@string/ApplicationName",
	Icon = "@mipmap/icon",
	LargeHeap = true,
	HardwareAccelerated = true,
	Theme = "@style/AppTheme"
)]
public class Application : Uno.UI.Runtime.Android.NativeApplication
{
	static Application()
	{
	}

	public Application(IntPtr javaReference, JniHandleOwnership transfer)
		: base(javaReference, transfer)
	{
	}

	protected override UnoPlatformHost CreateHost() =>
		UnoPlatformHostBuilder.Create()
			.App(() => new MZikmundApp())
			.UseAndroid()
			.Build();
}

