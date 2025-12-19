using System.Globalization;
using MZikmund.ViewModels.Admin;
using Windows.Foundation;

namespace MZikmund.Views.Admin;

public sealed partial class PostEditorView : PostEditorViewBase
{
	private readonly WebView2 _previewWebView;

	private string _postPreviewTemplate;

	public PostEditorView()
	{
		InitializeComponent();
		// Read the template from the embedded resource
		_postPreviewTemplate = typeof(PostEditorView).Assembly.GetManifestResourceStream("MZikmund.App.Templates.PostPreviewTemplate.html")!.ReadToEnd()!;
		PreviewWebViewContainer.Content = _previewWebView = new WebView2();
		this.Loaded += PostEditorView_Loaded;
		this.Unloaded += PostEditorView_Unloaded;
		ContentTextBox.SelectionChanged += ContentTextBox_SelectionChanged;
	}

	private void PostEditorView_Unloaded(object sender, RoutedEventArgs e)
	{
		ViewModel!.PropertyChanged -= ViewModel_PropertyChanged;
		ContentTextBox.SelectionChanged -= ContentTextBox_SelectionChanged;
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

	private async void ContentTextBox_SelectionChanged(object sender, RoutedEventArgs e)
	{
		if (_previewWebView?.CoreWebView2 is null)
		{
			return;
		}

		// Calculate the current line number from the caret position
		var text = ContentTextBox.Text;
		var caretPosition = ContentTextBox.SelectionStart;
		var lineNumber = GetLineNumberFromPosition(text, caretPosition);

		// Call JavaScript to scroll to the corresponding element
		try
		{
			await _previewWebView.ExecuteScriptAsync($"scrollToSourceLine({lineNumber})");
		}
		catch
		{
			// Ignore errors if the script fails (e.g., during navigation)
		}
	}

	private static int GetLineNumberFromPosition(string text, int position)
	{
		if (string.IsNullOrEmpty(text) || position <= 0)
		{
			return 0;
		}

		var lineNumber = 0;
		for (var i = 0; i < Math.Min(position, text.Length); i++)
		{
			if (text[i] == '\r')
			{
				lineNumber++;
			}
		}

		return lineNumber;
	}

	private void UpdatePreview()
	{
		var preview = _postPreviewTemplate
			.Replace("{POSTCONTENT}", ViewModel!.HtmlPreview)
			.Replace("{ACTUALTHEME}", ActualTheme == ElementTheme.Light ? "light" : "dark");
		_previewWebView.NavigateToString(preview);
	}
}

public partial class PostEditorViewBase : PageBase<PostEditorViewModel>
{
}
