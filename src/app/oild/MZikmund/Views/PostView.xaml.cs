using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using MZikmund.ViewModels;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace MZikmund.Views;

public sealed partial class PostView : PostViewBase
{
	private readonly WebView2 _previewWebView;

	private string _postPreviewTemplate;

	public PostView()
	{
		InitializeComponent();
		// Read the template from the embedded resource
		_postPreviewTemplate = typeof(PostView).GetAssembly().GetManifestResourceStream("MZikmund.Assets.PostPreviewTemplate.html")?.ReadToEnd()!;
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

public partial class PostViewBase : PageBase<PostViewModel>
{
}
