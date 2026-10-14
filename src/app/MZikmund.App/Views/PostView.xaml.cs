using MZikmund.ViewModels;
using Uno.Extensions;

namespace MZikmund.Views;

public sealed partial class PostView : PostViewBase
{
	// Fully qualified: Uno 6.8 added a root-level WebView2 namespace that shadows the simple name.
	private readonly Microsoft.UI.Xaml.Controls.WebView2 _webView;
	private bool _isWebViewReady;

	public PostView()
	{
		InitializeComponent();
		WebViewContainer.Content = _webView = new Microsoft.UI.Xaml.Controls.WebView2();
		_webView.NavigationCompleted += WebView_NavigationCompleted;
		Loaded += PostView_Loaded;
		Unloaded += PostView_Unloaded;
	}

	private async void PostView_Loaded(object sender, RoutedEventArgs e)
	{
		if (ViewModel is null)
		{
			return;
		}

		ViewModel.PropertyChanged += ViewModel_PropertyChanged;
		try
		{
			await _webView.EnsureCoreWebView2Async();
			_isWebViewReady = true;
			ApplyColorScheme();
			NavigateToPost();
		}
		catch (Exception ex)
		{
			this.Log().LogError(ex, "Initializing the post WebView failed");
			ViewModel.OnContentLoaded(isSuccess: false);
		}
	}

	private void PostView_Unloaded(object sender, RoutedEventArgs e)
	{
		if (ViewModel is not null)
		{
			ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
		}
	}

	private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(PostViewModel.EmbedUrl))
		{
			NavigateToPost();
		}
	}

	private void WebView_NavigationCompleted(Microsoft.UI.Xaml.Controls.WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs args) =>
		ViewModel?.OnContentLoaded(args.IsSuccess);

	private void NavigateToPost()
	{
		if (_isWebViewReady && ViewModel?.EmbedUrl is { } url)
		{
			_webView.Source = url;
		}
	}

	// The embed follows prefers-color-scheme; keep it in step with the app rather than the OS.
	private void ApplyColorScheme()
	{
#if WINDOWS
		_webView.CoreWebView2.Profile.PreferredColorScheme = ActualTheme == ElementTheme.Dark ?
			Microsoft.Web.WebView2.Core.CoreWebView2PreferredColorScheme.Dark :
			Microsoft.Web.WebView2.Core.CoreWebView2PreferredColorScheme.Light;
#endif
	}
}

public partial class PostViewBase : PageBase<PostViewModel>
{
}
