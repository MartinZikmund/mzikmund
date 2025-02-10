using Android.Runtime;
using Com.Nostra13.Universalimageloader.Core;

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
		ConfigureUniversalImageLoader();
	}

	private static void ConfigureUniversalImageLoader()
	{
		// Create global configuration and initialize ImageLoader with this config
		ImageLoaderConfiguration config = new ImageLoaderConfiguration
			.Builder(Context)
			.Build();

		ImageLoader.Instance.Init(config);

		ImageSource.DefaultImageLoader = ImageLoader.Instance.LoadImageAsync;
	}
}

