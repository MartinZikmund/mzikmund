using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Imaging;
using MZikmund.ViewModels.Admin;
using Uno.Extensions;

namespace MZikmund.Views.Admin;

public sealed partial class PostEditorView : PostEditorViewBase
{
	private const double WideLayoutMinWidth = 1100;
	private const double MediumLayoutMinWidth = 760;

	// Fully qualified: Uno 6.8 added a root-level WebView2 namespace that shadows the simple name.
	private readonly Microsoft.UI.Xaml.Controls.WebView2 _previewWebView;
	private readonly string _postPreviewTemplate;
	private bool _hasInitialLayout;

	public PostEditorView()
	{
		InitializeComponent();
		_postPreviewTemplate = typeof(PostEditorView).Assembly.GetManifestResourceStream("MZikmund.App.Templates.PostPreviewTemplate.html")!.ReadToEnd()!;
		PreviewWebViewContainer.Content = _previewWebView = new Microsoft.UI.Xaml.Controls.WebView2();
		Loaded += PostEditorView_Loaded;
		Unloaded += PostEditorView_Unloaded;
		SizeChanged += (_, _) => UpdateLayoutState();
	}

	private async void PostEditorView_Loaded(object sender, RoutedEventArgs e)
	{
		if (!_hasInitialLayout)
		{
			// Wide windows fit everything; medium ones keep the details beside the editor; narrow ones just write.
			_hasInitialLayout = true;
			PreviewToggle.IsChecked = ActualWidth >= WideLayoutMinWidth;
			DetailsToggle.IsChecked = ActualWidth >= MediumLayoutMinWidth;
		}

		UpdateLayoutState();

		if (ViewModel is null)
		{
			return;
		}

		ViewModel.PropertyChanged += ViewModel_PropertyChanged;
		ViewModel.UpdateSelectionRequested += OnUpdateSelectionRequested;
		try
		{
			_previewWebView.CoreWebView2Initialized += OnWebViewInitialized;
			await _previewWebView.EnsureCoreWebView2Async();
		}
		catch (Exception ex)
		{
			this.Log().LogError(ex, "Failed to initialize WebView2 in PostEditorView");
		}
	}

	private void PostEditorView_Unloaded(object sender, RoutedEventArgs e)
	{
		if (ViewModel is not null)
		{
			ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
			ViewModel.UpdateSelectionRequested -= OnUpdateSelectionRequested;
		}

		_previewWebView.CoreWebView2Initialized -= OnWebViewInitialized;
	}

	private void LayoutToggle_Click(object sender, RoutedEventArgs e)
	{
		// Below the wide layout only one secondary pane fits, so the toggles are exclusive.
		if (ActualWidth < WideLayoutMinWidth && sender is ToggleButton { IsChecked: true } toggled)
		{
			(toggled == PreviewToggle ? DetailsToggle : PreviewToggle).IsChecked = false;
		}

		UpdateLayoutState();
	}

	private void UpdateLayoutState()
	{
		var isWide = ActualWidth >= WideLayoutMinWidth;
		var isNarrow = ActualWidth < MediumLayoutMinWidth;
		var showPreview = PreviewToggle.IsChecked == true;
		var showDetails = DetailsToggle.IsChecked == true;

		if (!isWide && showPreview && showDetails)
		{
			showPreview = false;
			PreviewToggle.IsChecked = false;
		}

		// Narrow: a secondary pane replaces the editor instead of squeezing in beside it.
		var showEditor = !isNarrow || (!showPreview && !showDetails);

		EditorPane.Visibility = showEditor ? Visibility.Visible : Visibility.Collapsed;
		EditorColumn.Width = showEditor ? new GridLength(1, GridUnitType.Star) : new GridLength(0);

		PreviewPane.Visibility = showPreview ? Visibility.Visible : Visibility.Collapsed;
		PreviewPane.Margin = showEditor ? new Thickness(0, 16, 16, 16) : new Thickness(16);
		PreviewColumn.Width = showPreview ? new GridLength(1, GridUnitType.Star) : new GridLength(0);

		DetailsPane.Visibility = showDetails ? Visibility.Visible : Visibility.Collapsed;
		DetailsColumn.Width = !showDetails ? new GridLength(0) :
			isNarrow ? new GridLength(1, GridUnitType.Star) : new GridLength(340);
	}

	private void OnUpdateSelectionRequested(object? sender, EventArgs e) =>
		ContentTextBox.Select(ViewModel!.SelectionStart, ViewModel!.SelectionLength);

	private void OnWebViewInitialized(Microsoft.UI.Xaml.Controls.WebView2 sender, CoreWebView2InitializedEventArgs args) => UpdatePreview();

	private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(PostEditorViewModel.HtmlPreview))
		{
			UpdatePreview();
		}
	}

	private void ContentTextBox_SelectionChanged(object sender, RoutedEventArgs e) =>
		ViewModel?.SetSelection(ContentTextBox.SelectionStart, ContentTextBox.SelectionLength);

	private void UpdatePreview()
	{
		if (ViewModel is null || _previewWebView.CoreWebView2 is null)
		{
			return;
		}

		var preview = _postPreviewTemplate
			.Replace("{POSTCONTENT}", ViewModel.HtmlPreview)
			.Replace("{ACTUALTHEME}", ActualTheme == ElementTheme.Light ? "light" : "dark");
		_previewWebView.NavigateToString(preview);
	}

	public static ImageSource? ToImage(string? url) =>
		Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" ? new BitmapImage(uri) : null;
}

public partial class PostEditorViewBase : PageBase<PostEditorViewModel>
{
}
