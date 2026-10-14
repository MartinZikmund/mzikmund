using Microsoft.UI.Xaml.Media.Imaging;
using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Services.Localization;
using MZikmund.ViewModels;
using MZikmund.ViewModels.Items;

namespace MZikmund.Views;

/// <summary>
/// The comments panel of a post. Not a page: the host (PostView) creates the view model and assigns <see cref="ViewModel"/>.
/// </summary>
public sealed partial class CommentsView : UserControl
{
	public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
		nameof(ViewModel), typeof(CommentsViewModel), typeof(CommentsView), new PropertyMetadata(null, OnViewModelChanged));

	private bool _isLoaded;

	public CommentsView()
	{
		InitializeComponent();
		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
	}

	public CommentsViewModel? ViewModel
	{
		get => (CommentsViewModel?)GetValue(ViewModelProperty);
		set => SetValue(ViewModelProperty, value);
	}

	private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var view = (CommentsView)d;
		if (view._isLoaded)
		{
			(e.OldValue as CommentsViewModel)?.Detach();
			(e.NewValue as CommentsViewModel)?.Attach();
		}

		view.Bindings.Update();
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		_isLoaded = true;
		ViewModel?.Attach();
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		_isLoaded = false;
		ViewModel?.Detach();
	}

	private void SortTop_Click(object sender, RoutedEventArgs e) => ViewModel?.SetSort(CommentSortOrder.Top);

	private void SortNew_Click(object sender, RoutedEventArgs e) => ViewModel?.SetSort(CommentSortOrder.New);

	// x:Bind templates do not get a DataContext on WinUI's ItemsRepeater, so every button carries its item in Tag.
	private static CommentItemViewModel? ItemOf(object sender) => (sender as FrameworkElement)?.Tag as CommentItemViewModel;

	private async void Upvote_Click(object sender, RoutedEventArgs e)
	{
		if (ViewModel is { } viewModel && ItemOf(sender) is { } item)
		{
			await viewModel.ToggleUpvoteAsync(item);
		}
	}

	private void Reply_Click(object sender, RoutedEventArgs e)
	{
		if (ViewModel is { } viewModel && ItemOf(sender) is { } item)
		{
			viewModel.BeginReply(item);
		}
	}

	private async void MoreReplies_Click(object sender, RoutedEventArgs e)
	{
		if (ViewModel is { } viewModel && ItemOf(sender) is { } item)
		{
			await viewModel.LoadMoreRepliesAsync(item);
		}
	}

	private async void SaveEdit_Click(object sender, RoutedEventArgs e)
	{
		if (ViewModel is { } viewModel && ItemOf(sender) is { } item)
		{
			await viewModel.SaveEditAsync(item);
		}
	}

	private void CancelEdit_Click(object sender, RoutedEventArgs e)
	{
		if (ViewModel is { } viewModel && ItemOf(sender) is { } item)
		{
			viewModel.CancelEdit(item);
		}
	}

	// Built on click: flyout items cannot bind to the page, and which entries show depends on the item.
	private void More_Click(object sender, RoutedEventArgs e)
	{
		if (ViewModel is not { } viewModel || sender is not FrameworkElement target || ItemOf(sender) is not { } item)
		{
			return;
		}

		MenuFlyout flyout = new();
		if (item.IsOwn)
		{
			flyout.Items.Add(CreateMenuItem("CommentEdit", () => viewModel.BeginEdit(item)));
			flyout.Items.Add(CreateMenuItem("Delete", async () => await viewModel.DeleteAsync(item)));
		}
		else
		{
			flyout.Items.Add(CreateMenuItem("CommentReport", async () => await viewModel.ReportAsync(item)));
		}

		flyout.ShowAt(target);
	}

	private static MenuFlyoutItem CreateMenuItem(string textKey, Action onClick)
	{
		MenuFlyoutItem menuItem = new() { Text = Localizer.Instance.GetString(textKey) };
		menuItem.Click += (_, _) => onClick();
		return menuItem;
	}

	public Style SortStyle(bool isSelected) =>
		(Style)(isSelected ? Resources["CommentSortSelectedStyle"] : Application.Current.Resources["MzTagPillButtonStyle"]);

	public static Style UpvoteStyle(bool hasUpvoted) =>
		(Style)Application.Current.Resources[hasUpvoted ? "MzAccentSubtleButtonSmallStyle" : "MzSubtleButtonSmallStyle"];

	public static string UpvoteGlyph(bool hasUpvoted) =>
		(string)Application.Current.Resources[hasUpvoted ? "MzGlyphArrowUpFilled" : "MzGlyphArrowUp"];

	public static Visibility HasReplies(int replyCount, bool hasMoreReplies) =>
		replyCount > 0 || hasMoreReplies ? Visibility.Visible : Visibility.Collapsed;

	// Decoded at avatar size; the list is virtualized, so only visible avatars are fetched.
	public static ImageSource? ToAvatar(Uri? avatarUri) =>
		avatarUri is null ? null : new BitmapImage(avatarUri) { DecodePixelWidth = 64, DecodePixelType = DecodePixelType.Logical };
}
