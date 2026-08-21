using Humanizer;
using Microsoft.UI.Xaml.Media.Imaging;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Core.Services;

namespace MZikmund.ViewModels.Items;

public sealed class PostListItemViewModel : ObservableObject, ICardItem
{
	private ImageSource? _cardImage;

	public PostListItemViewModel(PostListItem item, IMarkdownConverter markdownConverter)
	{
		Item = item;
		HeroImageUri = item.HeroImageUrl is { } heroImageUrl ? new Uri(heroImageUrl) : null;
		CardSummary = markdownConverter.ToPlainText(item.Abstract);
		CardMeta = (item.PublishedDate ?? item.LastModifiedDate)?.Humanize() ?? "";
		CardBadges = item.Categories.Select(category => category.DisplayName).ToArray();
	}

	public PostListItem Item { get; }

	public Uri? HeroImageUri { get; }

	// Creating the bitmap starts a download, so it waits until the card is actually realized.
	public ImageSource? CardImage => _cardImage ??= HeroImageUri is { } uri ? new BitmapImage(uri) : null;

	public bool HasCardImage => HeroImageUri is not null;

	public string CardTitle => Item.Title;

	public string CardSummary { get; }

	public string CardMeta { get; }

	public IReadOnlyList<string> CardBadges { get; }
}
