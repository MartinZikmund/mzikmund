using Android.App;
using Android.Content;
using Android.Content.PM;
using Uno.AuthenticationBroker;

namespace MZikmund.App.Droid;

[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(
	[Intent.ActionView],
	Categories = [Intent.CategoryDefault, Intent.CategoryBrowsable],
	DataScheme = "mzikmund-app",
	DataHost = "oauthcallback")]
public class WebAuthenticationBrokerActivity : WebAuthenticationBrokerActivityBase
{
}
