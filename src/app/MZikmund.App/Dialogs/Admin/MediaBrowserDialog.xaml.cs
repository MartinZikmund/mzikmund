using MZikmund.App.Core.ViewModels.Admin;
using MZikmund.ViewModels.Admin;
using Microsoft.UI.Xaml;
using MZikmund.Dialogs;

namespace MZikmund.App.Dialogs.Admin;

public sealed partial class MediaBrowserDialog : MediaBrowserDialogBase
{
	public MediaBrowserDialog()
	{
		InitializeComponent();
		Loaded += OnLoaded;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		if (ViewModel != null)
		{
			ViewModel.PropertyChanged += ViewModel_PropertyChanged;
			UpdateSelectButtonState();
			UpdateUIVisibility();
		}
	}

	private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(ViewModel.SelectedFile))
		{
			UpdateSelectButtonState();
		}
		else if (e.PropertyName == nameof(ViewModel.IsLoading))
		{
			UpdateUIVisibility();
		}
	}

	private void UpdateSelectButtonState()
	{
		SelectButton.IsEnabled = ViewModel?.SelectedFile != null;
	}

	private void UpdateUIVisibility()
	{
		if (ViewModel != null)
		{
			FilesListView.Visibility = ViewModel.IsLoading ? Visibility.Collapsed : Visibility.Visible;
			LoadingRing.Visibility = ViewModel.IsLoading ? Visibility.Visible : Visibility.Collapsed;
		}
	}

	private void CancelButton_Click(object sender, RoutedEventArgs e)
	{
	}

	private void SelectButton_Click(object sender, RoutedEventArgs e)
	{
	}
}

public abstract partial class MediaBrowserDialogBase : DialogBase<MediaBrowserDialogViewModel>
{
}
