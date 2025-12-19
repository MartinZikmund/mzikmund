using System.Globalization;
using MZikmund.ViewModels.Admin;
using Windows.Foundation;

namespace MZikmund.Views.Admin;

public sealed partial class PostEditorView : PostEditorViewBase
{
	private readonly WebView2 _previewWebView;

	private string _postPreviewTemplate;
	private int _lastCaretPosition = 0;

	public PostEditorView()
	{
		InitializeComponent();
		// Read the template from the embedded resource
		_postPreviewTemplate = typeof(PostEditorView).Assembly.GetManifestResourceStream("MZikmund.App.Templates.PostPreviewTemplate.html")!.ReadToEnd()!;
		PreviewWebViewContainer.Content = _previewWebView = new WebView2();
		this.Loaded += PostEditorView_Loaded;
		this.Unloaded += PostEditorView_Unloaded;
	}

	private void PostEditorView_Unloaded(object sender, RoutedEventArgs e)
	{
		ViewModel!.PropertyChanged -= ViewModel_PropertyChanged;
	}

	private async void PostEditorView_Loaded(object sender, RoutedEventArgs e)
	{
		try
		{
			_previewWebView.CoreWebView2Initialized += OnWebViewInitialized;
			await _previewWebView.EnsureCoreWebView2Async();
			ViewModel!.PropertyChanged += ViewModel_PropertyChanged;
		}
		catch (Exception ex)
		{
			if (XamlRoot?.Content is WindowShell windowShell)
			{
				var logger = windowShell.ServiceProvider.GetRequiredService<ILogger<PostEditorView>>();
				logger.LogError(ex, "Failed to initialize WebView2 in PostEditorView");
			}
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

	private async void UpdatePreview()
	{
		var preview = _postPreviewTemplate
			.Replace("{POSTCONTENT}", ViewModel!.HtmlPreview)
			.Replace("{ACTUALTHEME}", ActualTheme == ElementTheme.Light ? "light" : "dark");
		_previewWebView.NavigateToString(preview);
		
		// Wait a bit for content to load, then sync scroll position
		await Task.Delay(100);
		await SyncScrollPositionAsync();
	}

	private void ContentTextBox_SelectionChanged(object sender, RoutedEventArgs e)
	{
		if (ContentTextBox.SelectionStart != _lastCaretPosition)
		{
			_lastCaretPosition = ContentTextBox.SelectionStart;
			_ = SyncScrollPositionAsync();
		}
	}

	private async Task SyncScrollPositionAsync()
	{
		try
		{
			if (_previewWebView?.CoreWebView2 == null || string.IsNullOrEmpty(ViewModel?.PostContent))
			{
				return;
			}

			var contentLength = ViewModel.PostContent.Length;
			if (contentLength == 0)
			{
				return;
			}

			// Calculate the scroll position as a percentage based on caret position
			var scrollPercentage = (double)_lastCaretPosition / contentLength;

			// Inject JavaScript to scroll to the corresponding position
			var script = $@"
				(function() {{
					var scrollHeight = document.documentElement.scrollHeight - window.innerHeight;
					var targetScrollTop = scrollHeight * {scrollPercentage.ToString(CultureInfo.InvariantCulture)};
					window.scrollTo({{ top: targetScrollTop, behavior: 'smooth' }});
				}})();
			";

			await _previewWebView.ExecuteScriptAsync(script);
		}
		catch (Exception ex)
		{
			// Log error but don't throw - scrolling is a nice-to-have feature
			if (XamlRoot?.Content is WindowShell windowShell)
			{
				var logger = windowShell.ServiceProvider.GetRequiredService<ILogger<PostEditorView>>();
				logger.LogError(ex, "Failed to sync scroll position in PostEditorView");
			}
		}
	}
}

public partial class PostEditorViewBase : PageBase<PostEditorViewModel>
{
}
