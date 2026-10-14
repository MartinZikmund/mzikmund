using Microsoft.UI.Xaml.Media;

namespace MZikmund.ViewModels.Items;

/// <summary>
/// Data for <c>MzContentCardStyle</c> (Theme/Styles/Cards.xaml), supplied through DataContext.
/// </summary>
public interface ICardItem
{
	ImageSource? CardImage { get; }

	bool HasCardImage { get; }

	string CardTitle { get; }

	string CardSummary { get; }

	string CardMeta { get; }

	IReadOnlyList<string> CardBadges { get; }

	bool IsVideo { get; }
}
