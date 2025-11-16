using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MZikmund.ViewModels;

namespace MZikmund.Views;

public sealed partial class PostView : PostViewBase
{
	private readonly WebView2 _previewWebView;

	public PostView()
	{
		InitializeComponent();
		PreviewWebViewContainer.Content = _previewWebView = new WebView2();
		Loaded += PostView_Loaded;
		Unloaded += PostView_Unloaded;
	}

	private void PostView_Unloaded(object sender, RoutedEventArgs e)
	{
		ViewModel!.PropertyChanged -= ViewModel_PropertyChanged;
	}

	private async void PostView_Loaded(object sender, RoutedEventArgs e)
	{
		try
		{
			_previewWebView.CoreWebView2Initialized += OnWebViewInitialized;
			await _previewWebView.EnsureCoreWebView2Async();
			ViewModel!.PropertyChanged += ViewModel_PropertyChanged;
		}
		catch (Exception)
		{
			// TODO: Log error
		}
	}

	private void OnWebViewInitialized(WebView2 sender, CoreWebView2InitializedEventArgs args) => NavigateToPost();

	private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(ViewModel.EmbedUrl))
		{
			NavigateToPost();
		}
	}

	private void NavigateToPost()
	{
		if (!string.IsNullOrEmpty(ViewModel?.EmbedUrl) && _previewWebView.CoreWebView2 != null)
		{
			_previewWebView.Source = new Uri(ViewModel.EmbedUrl);
		}
	}
}

public partial class PostViewBase : PageBase<PostViewModel>
{
}
