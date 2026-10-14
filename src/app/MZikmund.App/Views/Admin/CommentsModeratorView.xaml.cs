using Microsoft.UI.Xaml.Automation;
using MZikmund.Services.Localization;
using MZikmund.ViewModels.Admin;

namespace MZikmund.Views.Admin;

public sealed partial class CommentsModeratorView : CommentsModeratorViewBase
{
	public CommentsModeratorView() => InitializeComponent();

	// The button carries its row in Tag because ItemsRepeater templates get no DataContext.
	private void RowMore_Click(object sender, RoutedEventArgs e)
	{
		if (ViewModel is not { } viewModel || sender is not FrameworkElement { Tag: CommentReportItemViewModel item } target)
		{
			return;
		}

		MenuFlyout flyout = new();
		if (item.CanHide)
		{
			flyout.Items.Add(CreateMenuItem("ModeratorHide", async () => await viewModel.HideAsync(item)));
		}

		flyout.Items.Add(CreateMenuItem("Delete", async () => await viewModel.DeleteAsync(item)));
		if (item.IsReport)
		{
			flyout.Items.Add(CreateMenuItem("ModeratorBan", async () => await viewModel.BanAsync(item)));
			flyout.Items.Add(CreateMenuItem("ModeratorDismiss", async () => await viewModel.DismissAsync(item)));
		}

		flyout.ShowAt(target);
	}

	private static MenuFlyoutItem CreateMenuItem(string textKey, Action onClick)
	{
		MenuFlyoutItem menuItem = new() { Text = Localizer.Instance.GetString(textKey) };
		AutomationProperties.SetAutomationId(menuItem, "ModeratorAction" + textKey.Replace("Moderator", ""));
		menuItem.Click += (_, _) => onClick();
		return menuItem;
	}
}

public partial class CommentsModeratorViewBase : PageBase<CommentsModeratorViewModel>
{
}
