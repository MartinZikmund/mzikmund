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
		else if (e.PropertyName == nameof(ViewModel.SelectionStart) || e.PropertyName == nameof(ViewModel.SelectionLength))
		{
			// Restore cursor position after inserting text
			ContentTextBox.Select(ViewModel!.SelectionStart, ViewModel.SelectionLength);
		}
	}

	private void ContentTextBox_SelectionChanged(object sender, RoutedEventArgs e)
	{
		// Update ViewModel with current cursor position and selection
		ViewModel!.SelectionStart = ContentTextBox.SelectionStart;
		ViewModel.SelectionLength = ContentTextBox.SelectionLength;
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
