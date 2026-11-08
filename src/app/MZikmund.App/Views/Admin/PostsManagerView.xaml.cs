using System.Globalization;
using Microsoft.UI.Xaml.Media.Imaging;
using MZikmund.DataContracts.Blog;
using MZikmund.Services.Localization;
using MZikmund.ViewModels.Admin;
using MZikmund.ViewModels.Items;

namespace MZikmund.Views.Admin;

public sealed partial class PostsManagerView : PostsManagerViewBase
{
	public PostsManagerView() => InitializeComponent();

	// x:Bind templates do not get a DataContext on WinUI's ItemsRepeater, so the row carries its item in Tag.
	private void PostRow_Click(object sender, RoutedEventArgs e)
	{
		if (sender is FrameworkElement { Tag: AdminPostItemViewModel row })
		{
			ViewModel?.EditPost(row.Post);
		}
	}

	public static Visibility HasStatus(bool? isPublished) => isPublished.HasValue ? Visibility.Visible : Visibility.Collapsed;

	public static string StatusText(bool? isPublished, bool isScheduled) =>
		Localizer.Instance.GetString(isScheduled ? "Scheduled" : isPublished == true ? "Published" : "Draft");

	public static Style StatusStyle(bool? isPublished, bool isScheduled) =>
		(Style)Application.Current.Resources[isScheduled ? "MzBadgeAccentStyle" : isPublished == true ? "MzBadgeSubtleStyle" : "MzBadgeNeutralStyle"];

	public static string FormatSubtitle(string routeName, Category[] categories) =>
		categories is { Length: > 0 } ?
			$"/{routeName}  ·  {string.Join(", ", categories.Select(c => c.DisplayName))}" :
			$"/{routeName}";

	// The moment a scheduled post goes live matters to the minute; elsewhere the day is enough.
	public static string FormatDate(DateTimeOffset? publishedDate, DateTimeOffset? lastModifiedDate, bool isScheduled) =>
		(publishedDate ?? lastModifiedDate)?.ToLocalTime().ToString(isScheduled ? "g" : "d", CultureInfo.CurrentCulture) ?? "";

	// Decoded at thumbnail size; the rows are virtualized, so only visible heroes are fetched.
	public static ImageSource? ToThumbnail(string? heroImageUrl) =>
		Uri.TryCreate(heroImageUrl, UriKind.Absolute, out var uri) ?
			new BitmapImage(uri) { DecodePixelWidth = 192, DecodePixelType = DecodePixelType.Logical } :
			null;

	public static bool Any(bool first, bool second) => first || second;

	public static Visibility AnyVisible(bool first, bool second) => first || second ? Visibility.Visible : Visibility.Collapsed;
}

public partial class PostsManagerViewBase : PageBase<PostsManagerViewModel>
{
}
