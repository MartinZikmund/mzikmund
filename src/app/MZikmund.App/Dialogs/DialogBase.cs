using MZikmund.ViewModels;

namespace MZikmund.Dialogs;

public abstract partial class DialogBase<TDialogViewModel> : ContentDialog
	where TDialogViewModel : DialogViewModel
{
	protected DialogBase()
	{
		DataContextChanged += OnDataContextChanged;
		PrimaryButtonClick += OnPrimaryButtonClick;
		SecondaryButtonClick += OnSecondaryButtonClick;
		CloseButtonClick += OnCloseButtonClick;
		Opened += OnOpened;
	}

	public TDialogViewModel? ViewModel { get; private set; }

	private void OnOpened(ContentDialog sender, ContentDialogOpenedEventArgs args) =>
		ViewModel?.OnOpened(this);

	private void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) =>
		ViewModel?.OnPrimaryButtonClick(sender, args);

	private void OnSecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) =>
		ViewModel?.OnSecondaryButtonClick(sender, args);

	private void OnCloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) =>
		ViewModel?.OnCloseButtonClick(sender, args);

	private void OnDataContextChanged(Microsoft.UI.Xaml.DependencyObject sender, Microsoft.UI.Xaml.DataContextChangedEventArgs args) =>
		ViewModel = args.NewValue switch
		{
			TDialogViewModel viewModel => viewModel,
			{ } incompatibleViewModel => throw new InvalidOperationException(
				$"View model has incompatible type {incompatibleViewModel.GetType().Name}. Expected {typeof(TDialogViewModel).Name}"),
			_ => null
		};
}
