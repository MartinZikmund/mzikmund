using System.Globalization;
using Humanizer;
using Microsoft.UI.Xaml.Media.Imaging;
using MZikmund.DataContracts.Videos;

namespace MZikmund.ViewModels.Items;

public sealed class VideoCardViewModel : ICardItem
{
	private ImageSource? _cardImage;

	public VideoCardViewModel(VideoDto video)
	{
		Video = video;
		CardMeta = (DateTimeOffset.UtcNow - video.PublishedDate).TotalDays > 30 ?
			video.PublishedDate.ToString("MMM d, yyyy", CultureInfo.InvariantCulture) :
			video.PublishedDate.Humanize();
	}

	public VideoDto Video { get; }

	public Uri? Url => Uri.TryCreate(Video.YouTubeUrl, UriKind.Absolute, out var uri) ? uri : null;

	public ImageSource? CardImage => _cardImage ??=
		Uri.TryCreate(Video.ThumbnailUrl, UriKind.Absolute, out var uri) ? new BitmapImage(uri) : null;

	public bool HasCardImage => !string.IsNullOrEmpty(Video.ThumbnailUrl);

	public string CardTitle => Video.Title;

	public string CardSummary => Video.Description.Trim();

	public string CardMeta { get; }

	public IReadOnlyList<string> CardBadges { get; } = Array.Empty<string>();

	public bool IsVideo => true;
}
