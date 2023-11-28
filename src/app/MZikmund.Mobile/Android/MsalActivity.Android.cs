using Android.App;
using Android.Content.PM;
using Microsoft.Identity.Client;

namespace MZikmund.Droid;

[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(
	new[] { Android.Content.Intent.ActionView },
	Categories = new[] { Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable },
	DataHost = "auth",
	DataScheme = "msal7e13557a-4799-46b8-9e2b-0f31c41a051e")]
public class MsalActivity : BrowserTabActivity
{
}
