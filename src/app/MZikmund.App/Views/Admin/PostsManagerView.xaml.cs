using System.Globalization;
using MZikmund.DataContracts.Blog;
using MZikmund.Services.Localization;
using MZikmund.ViewModels.Admin;

namespace MZikmund.Views.Admin;

public sealed partial class PostsManagerView : PostsManagerViewBase
{
	public PostsManagerView() => InitializeComponent();

	// x:Bind templates do not get a DataContext on WinUI's ItemsRepeater, so the row carries its item in Tag.
	private void PostRow_Click(object sender, RoutedEventArgs e)
	{
		if (sender is FrameworkElement { Tag: PostListItem post })
		{
			ViewModel?.EditPost(post);
		}
	}

	public static Visibility HasStatus(bool? isPublished) => isPublished.HasValue ? Visibility.Visible : Visibility.Collapsed;

	public static string StatusText(bool? isPublished) =>
		Localizer.Instance.GetString(isPublished == true ? "Published" : "Draft");

	public static Style StatusStyle(bool? isPublished) =>
		(Style)Application.Current.Resources[isPublished == true ? "MzBadgeSubtleStyle" : "MzBadgeNeutralStyle"];

	public static string FormatSubtitle(string routeName, Category[] categories) =>
		categories is { Length: > 0 } ?
			$"/{routeName}  ·  {string.Join(", ", categories.Select(c => c.DisplayName))}" :
			$"/{routeName}";

	public static string FormatDate(DateTimeOffset? publishedDate, DateTimeOffset? lastModifiedDate) =>
		(publishedDate ?? lastModifiedDate)?.ToLocalTime().ToString("d", CultureInfo.CurrentCulture) ?? "";

	public static bool Any(bool first, bool second) => first || second;

	public static Visibility AnyVisible(bool first, bool second) => first || second ? Visibility.Visible : Visibility.Collapsed;
}

public partial class PostsManagerViewBase : PageBase<PostsManagerViewModel>
{
}
