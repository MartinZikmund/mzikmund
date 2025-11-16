using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MZikmund.ViewModels;

namespace MZikmund.Views;

public sealed partial class PostPreviewView : PostPreviewViewBase
{
	private readonly WebView2 _previewWebView;

	private string _postPreviewTemplate;

	public PostPreviewView()
	{
		InitializeComponent();
		// Read the template from the embedded resource
		using var stream = typeof(PostPreviewView).Assembly.GetManifestResourceStream("MZikmund.Templates.PostPreviewTemplate.html");
		using var reader = new StreamReader(stream!);
		_postPreviewTemplate = reader.ReadToEnd();
		PreviewWebViewContainer.Content = _previewWebView = new WebView2();
		Loaded += PostPreviewView_Loaded;
		Unloaded += PostPreviewView_Unloaded;
	}

	private void PostPreviewView_Unloaded(object sender, RoutedEventArgs e)
	{
		ViewModel!.PropertyChanged -= ViewModel_PropertyChanged;
	}

	private async void PostPreviewView_Loaded(object sender, RoutedEventArgs e)
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

	private void OnWebViewInitialized(WebView2 sender, CoreWebView2InitializedEventArgs args) => UpdatePreview();

	private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(ViewModel.HtmlPreview))
		{
			UpdatePreview();
		}
	}

	private void UpdatePreview()
	{
		_previewWebView.NavigateToString(string.Format(CultureInfo.InvariantCulture, _postPreviewTemplate, ViewModel!.HtmlPreview ?? ""));
	}
}

public partial class PostPreviewViewBase : PageBase<PostPreviewViewModel>
{
}
