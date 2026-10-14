using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Documents;
using MZikmund.Shared.Blog;

namespace MZikmund.Extensions.Xaml;

/// <summary>
/// Renders comment markdown into the inlines of a <see cref="TextBlock"/>. The parsed model only
/// contains what the server renderer allows, so there is no image or script path to guard here.
/// </summary>
public static class CommentText
{
	public static readonly DependencyProperty MarkdownProperty = DependencyProperty.RegisterAttached(
		"Markdown", typeof(string), typeof(CommentText), new PropertyMetadata(null, OnChanged));

	// Optional leading "@Name " run, drawn in the accent colour.
	public static readonly DependencyProperty PrefixProperty = DependencyProperty.RegisterAttached(
		"Prefix", typeof(string), typeof(CommentText), new PropertyMetadata(null, OnChanged));

	private static readonly DependencyProperty IsThemeHookedProperty = DependencyProperty.RegisterAttached(
		"IsThemeHooked", typeof(bool), typeof(CommentText), new PropertyMetadata(false));

	private static bool GetIsThemeHooked(DependencyObject element) => (bool)element.GetValue(IsThemeHookedProperty);

	private static void SetIsThemeHooked(DependencyObject element, bool value) => element.SetValue(IsThemeHookedProperty, value);

	public static string? GetMarkdown(DependencyObject element) => (string?)element.GetValue(MarkdownProperty);

	public static void SetMarkdown(DependencyObject element, string? value) => element.SetValue(MarkdownProperty, value);

	public static string? GetPrefix(DependencyObject element) => (string?)element.GetValue(PrefixProperty);

	public static void SetPrefix(DependencyObject element, string? value) => element.SetValue(PrefixProperty, value);

	private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is TextBlock textBlock)
		{
			Rebuild(textBlock);
		}
	}

	private static void Rebuild(TextBlock textBlock)
	{
		if (!GetIsThemeHooked(textBlock))
		{
			SetIsThemeHooked(textBlock, true);
			textBlock.ActualThemeChanged += (sender, _) => Rebuild((TextBlock)sender);
		}

		textBlock.Inlines.Clear();

		if (GetPrefix(textBlock) is { Length: > 0 } prefix)
		{
			textBlock.Inlines.Add(new Run
			{
				Text = prefix,
				FontWeight = FontWeights.SemiBold,
				Foreground = GetBrush(textBlock, "MzAccentTextBrush"),
			});
		}

		AppendBlocks(textBlock, textBlock.Inlines, CommentMarkdown.Parse(GetMarkdown(textBlock)));
	}

	private static void AppendBlocks(TextBlock owner, InlineCollection target, IReadOnlyList<CommentBlock> blocks)
	{
		for (var i = 0; i < blocks.Count; i++)
		{
			if (i > 0)
			{
				target.Add(new LineBreak());
				if (!(blocks[i] is ListItemBlock && blocks[i - 1] is ListItemBlock))
				{
					target.Add(new LineBreak());
				}
			}

			switch (blocks[i])
			{
				case ParagraphBlock paragraph:
					AppendInlines(target, paragraph.Inlines);
					break;

				case CodeBlock code:
					target.Add(new Run { Text = code.Code, FontFamily = GetMono() });
					break;

				case QuoteBlock quote:
					Span quoteSpan = new() { Foreground = GetBrush(owner, "MzTextSecondaryBrush") };
					quoteSpan.Inlines.Add(new Run { Text = "│ " });
					AppendBlocks(owner, quoteSpan.Inlines, quote.Blocks);
					target.Add(quoteSpan);
					break;

				case ListItemBlock item:
					target.Add(new Run { Text = item.Marker + " " });
					AppendBlocks(owner, target, item.Blocks);
					break;
			}
		}
	}

	private static void AppendInlines(InlineCollection target, IReadOnlyList<CommentInline> inlines)
	{
		foreach (var inline in inlines)
		{
			switch (inline)
			{
				case TextInline text:
					target.Add(new Run { Text = text.Text });
					break;

				case StrongInline strong:
					// The Inter faces are one file per weight; a bare Bold has no face to pick on Skia.
					Bold bold = new() { FontFamily = (FontFamily)Application.Current.Resources["MzFontFamilySemiBold"] };
					AppendInlines(bold.Inlines, strong.Children);
					target.Add(bold);
					break;

				case EmphasisInline emphasis:
					Italic italic = new();
					AppendInlines(italic.Inlines, emphasis.Children);
					target.Add(italic);
					break;

				case StrikeInline strike:
					Span strikeSpan = new() { TextDecorations = Windows.UI.Text.TextDecorations.Strikethrough };
					AppendInlines(strikeSpan.Inlines, strike.Children);
					target.Add(strikeSpan);
					break;

				case CodeInline code:
					target.Add(new Run { Text = code.Code, FontFamily = GetMono() });
					break;

				case LinkInline link:
					Hyperlink hyperlink = new() { NavigateUri = link.Url };
					AppendInlines(hyperlink.Inlines, link.Children);
					target.Add(hyperlink);
					break;

				case LineBreakInline:
					target.Add(new LineBreak());
					break;
			}
		}
	}

	private static FontFamily GetMono() => (FontFamily)Application.Current.Resources["MzMonoFontFamily"];

	// Application.Current.Resources follows the app theme, not the in-app override set on the window root.
	private static Brush? GetBrush(FrameworkElement owner, string key) =>
		FindThemed(Application.Current.Resources, owner.ActualTheme == ElementTheme.Dark ? "Dark" : "Light", key) as Brush;

	private static object? FindThemed(ResourceDictionary dictionary, string theme, string key)
	{
		if (dictionary.ThemeDictionaries.TryGetValue(theme, out var themed) &&
			themed is ResourceDictionary themeDictionary &&
			themeDictionary.TryGetValue(key, out var value))
		{
			return value;
		}

		foreach (var merged in dictionary.MergedDictionaries)
		{
			if (FindThemed(merged, theme, key) is { } found)
			{
				return found;
			}
		}

		return null;
	}
}
