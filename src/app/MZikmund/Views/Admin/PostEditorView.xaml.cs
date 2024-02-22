using MZikmund.ViewModels.Admin;

namespace MZikmund.Views.Admin;

public sealed partial class PostEditorView : PostEditorViewBase
{
	public PostEditorView()
	{
		InitializeComponent();
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
			await PreviewWebView.EnsureCoreWebView2Async();
			ViewModel!.PropertyChanged += ViewModel_PropertyChanged;
		}
		catch (Exception)
		{
			// TODO: Log error
		}
	}

	private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(ViewModel.Post))
		{
			PostEditorWebView.NavigateToString(ViewModel.Post?.Content ?? "");
		}
	}
}

public partial class PostEditorViewBase : PageBase<PostEditorViewModel>
{
}
