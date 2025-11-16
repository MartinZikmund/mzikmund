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

	public PostView()
	{
		InitializeComponent();
		PreviewWebViewContainer.Content = _previewWebView = new WebView2();
		this.Loaded += PostView_Loaded;
		this.Unloaded += PostView_Unloaded;
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

	private void OnWebViewInitialized(WebView2 sender, CoreWebView2InitializedEventArgs args) => NavigateToChromelessUrl();

	private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(ViewModel.ChromelessUrl))
		{
			NavigateToChromelessUrl();
		}
	}

	private void NavigateToChromelessUrl()
	{
		if (!string.IsNullOrEmpty(ViewModel!.ChromelessUrl))
		{
			_previewWebView.Source = new Uri(ViewModel.ChromelessUrl);
		}
	}
}

public partial class PostViewBase : PageBase<PostViewModel>
{
}
