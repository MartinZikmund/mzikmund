using System.Globalization;
using System.Text.Encodings.Web;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Web.WebView2.Core;
using MZikmund.ViewModels.Admin;
using MZikmund.Web.Core.Services;
using Uno.Extensions;

namespace MZikmund.Views.Admin;

public sealed partial class PostEditorView : PostEditorViewBase
{
	private const double WideLayoutMinWidth = 1100;
	private const double MediumLayoutMinWidth = 760;
	private const double DefaultCaretRatio = 0.35;

	// Fully qualified: Uno 6.8 added a root-level WebView2 namespace that shadows the simple name.
	private readonly Microsoft.UI.Xaml.Controls.WebView2 _previewWebView;
	private readonly string _postPreviewTemplate;
	private bool _hasInitialLayout;
	private bool _isPreviewReady;
	private bool _isPushingPreview;
	private bool _isContentPushPending;
	private bool _isSyncPending;
	private bool _isCaretRectUnsupported;

	public PostEditorView()
	{
		InitializeComponent();
		_postPreviewTemplate = typeof(PostEditorView).Assembly.GetManifestResourceStream("MZikmund.App.Templates.PostPreviewTemplate.html")!.ReadToEnd()!;
		PreviewWebViewContainer.Content = _previewWebView = new Microsoft.UI.Xaml.Controls.WebView2
		{
			// Lets the pane's surface show through instead of a white flash before the styles load.
			DefaultBackgroundColor = Microsoft.UI.Colors.Transparent,
		};
		Loaded += PostEditorView_Loaded;
		Unloaded += PostEditorView_Unloaded;
		SizeChanged += (_, _) => UpdateLayoutState();
		ActualThemeChanged += (_, _) => LoadPreviewTemplate();
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
			_previewWebView.NavigationCompleted += OnPreviewNavigationCompleted;
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
		_previewWebView.NavigationCompleted -= OnPreviewNavigationCompleted;
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

	private void OnWebViewInitialized(Microsoft.UI.Xaml.Controls.WebView2 sender, CoreWebView2InitializedEventArgs args) => LoadPreviewTemplate();

	private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(PostEditorViewModel.HtmlPreview))
		{
			_isContentPushPending = true;
			_ = PushPreviewAsync();
		}
	}

	private void ContentTextBox_SelectionChanged(object sender, RoutedEventArgs e)
	{
		ViewModel?.SetSelection(ContentTextBox.SelectionStart, ContentTextBox.SelectionLength);
		_isSyncPending = true;
		_ = PushPreviewAsync();
	}

	// The page is loaded once per theme; edits are swapped in by script so the preview never reloads.
	private void LoadPreviewTemplate()
	{
		if (_previewWebView.CoreWebView2 is null)
		{
			return;
		}

		_isPreviewReady = false;
		_previewWebView.NavigateToString(
			_postPreviewTemplate.Replace("{ACTUALTHEME}", ActualTheme == ElementTheme.Light ? "light" : "dark"));
	}

	private void OnPreviewNavigationCompleted(Microsoft.UI.Xaml.Controls.WebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
	{
		_isPreviewReady = true;
		_isContentPushPending = true;
		_ = PushPreviewAsync();
	}

	// One script at a time; requests arriving meanwhile collapse into a single follow-up with the latest state.
	private async Task PushPreviewAsync()
	{
		if (_isPushingPreview || !_isPreviewReady || ViewModel is null)
		{
			return;
		}

		_isPushingPreview = true;
		try
		{
			while (_isPreviewReady && (_isContentPushPending || _isSyncPending))
			{
				var caret = GetCaretArguments();
				var script = _isContentPushPending ?
					$"mzPreview.update(\"{JavaScriptEncoder.Default.Encode(ViewModel.HtmlPreview)}\", {caret})" :
					$"mzPreview.sync({caret})";
				_isContentPushPending = false;
				_isSyncPending = false;
				await _previewWebView.ExecuteScriptAsync(script);
			}
		}
		catch (Exception ex)
		{
			this.Log().LogWarning(ex, "Updating the post preview failed");
		}
		finally
		{
			_isPushingPreview = false;
		}
	}

	private string GetCaretArguments()
	{
		var text = ContentTextBox.Text ?? "";
		var line = MarkdownSourceLines.LineAt(text, ContentTextBox.SelectionStart);
		var totalLines = MarkdownSourceLines.LineAt(text, text.Length) + 1;
		return string.Create(CultureInfo.InvariantCulture, $"{line}, {totalLines}, {GetCaretViewportRatio(text):0.###}");
	}

	// Where the caret sits in the editor's viewport, so the preview can put the same content at the same height.
	private double GetCaretViewportRatio(string text)
	{
		if (_isCaretRectUnsupported || text.Length == 0 || ContentTextBox.ActualHeight <= 0)
		{
			return DefaultCaretRatio;
		}

		try
		{
			var atEnd = ContentTextBox.SelectionStart >= text.Length;
			var rect = ContentTextBox.GetRectFromCharacterIndex(atEnd ? text.Length - 1 : ContentTextBox.SelectionStart, atEnd);
			var ratio = rect.Y / ContentTextBox.ActualHeight;
			return double.IsFinite(ratio) ? Math.Clamp(ratio, 0.05, 0.9) : DefaultCaretRatio;
		}
		catch (ArgumentException)
		{
			return DefaultCaretRatio;
		}
		catch (NotImplementedException)
		{
			// Uno (Skia) does not implement it; asking on every keystroke would only throw again.
			_isCaretRectUnsupported = true;
			return DefaultCaretRatio;
		}
	}

	public static ImageSource? ToImage(string? url) =>
		Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" ? new BitmapImage(uri) : null;
}

public partial class PostEditorViewBase : PageBase<PostEditorViewModel>
{
}
