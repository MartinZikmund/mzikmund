using MZikmund.App.Core.ViewModels.Admin;
using MZikmund.ViewModels.Admin;
using Microsoft.UI.Xaml;

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
		}
	}

	private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(ViewModel.SelectedFile))
		{
			UpdateSelectButtonState();
		}
	}

	private void UpdateSelectButtonState()
	{
		SelectButton.IsEnabled = ViewModel?.SelectedFile != null;
	}

	private void CancelButton_Click(object sender, RoutedEventArgs e)
	{
		Result = ContentDialogResult.None;
	}

	private void SelectButton_Click(object sender, RoutedEventArgs e)
	{
		Result = ContentDialogResult.Primary;
	}
}

public abstract partial class MediaBrowserDialogBase : DialogBase&lt;MediaBrowserDialogViewModel&gt;
{
}