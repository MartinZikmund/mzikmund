using Microsoft.UI.Xaml.Media;

namespace MZikmund.ViewModels.Items;

/// <summary>
/// Contract for anything rendered by <c>MzContentCardStyle</c> (Theme/Styles/Cards.xaml). The
/// style's hover storyboard reaches the title and image by name inside the ControlTemplate, so
/// implementers just need to supply the data via DataContext.
/// </summary>
public interface ICardItem
{
	ImageSource? CardImage { get; }

	bool HasCardImage { get; }

	string CardTitle { get; }

	string CardSummary { get; }

	string CardMeta { get; }

	IReadOnlyList<string> CardBadges { get; }
}
