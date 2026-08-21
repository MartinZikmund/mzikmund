using System.Globalization;
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
		// Markdig ends every block with a newline, and a trailing one makes the card reserve an
		// empty line it never draws, which pushes the card past its grid cell.
		CardSummary = markdownConverter.ToPlainText(item.Abstract).Trim();
		CardMeta = FormatCardMeta(item.PublishedDate ?? item.LastModifiedDate);
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

	/// <summary>
	/// Formats the card date the way _PostListItem.cshtml does: the relative form for the first
	/// 30 days, an absolute date after that. The site renders that date under the server culture
	/// with a US pattern, so the app pins the invariant culture rather than pairing the pattern
	/// with Czech month names.
	/// </summary>
	private static string FormatCardMeta(DateTimeOffset? date)
	{
		if (date is not { } value)
		{
			return "";
		}

		return (DateTimeOffset.UtcNow - value).TotalDays > 30
			? value.ToString("MMM d, yyyy", CultureInfo.InvariantCulture)
			: value.Humanize();
	}
}
