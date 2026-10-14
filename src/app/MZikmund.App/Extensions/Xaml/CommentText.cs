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

	public static string? GetMarkdown(DependencyObject element) => (string?)element.GetValue(MarkdownProperty);

	public static void SetMarkdown(DependencyObject element, string? value) => element.SetValue(MarkdownProperty, value);

	public static string? GetPrefix(DependencyObject element) => (string?)element.GetValue(PrefixProperty);

	public static void SetPrefix(DependencyObject element, string? value) => element.SetValue(PrefixProperty, value);

	private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is not TextBlock textBlock)
		{
			return;
		}

		textBlock.Inlines.Clear();

		if (GetPrefix(textBlock) is { Length: > 0 } prefix)
		{
			textBlock.Inlines.Add(new Run
			{
				Text = prefix,
				FontWeight = FontWeights.SemiBold,
				Foreground = GetBrush("MzAccentTextBrush"),
			});
		}

		AppendBlocks(textBlock.Inlines, CommentMarkdown.Parse(GetMarkdown(textBlock)));
	}

	private static void AppendBlocks(InlineCollection target, IReadOnlyList<CommentBlock> blocks)
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
					Span quoteSpan = new() { Foreground = GetBrush("MzTextSecondaryBrush") };
					quoteSpan.Inlines.Add(new Run { Text = "│ " });
					AppendBlocks(quoteSpan.Inlines, quote.Blocks);
					target.Add(quoteSpan);
					break;

				case ListItemBlock item:
					target.Add(new Run { Text = item.Marker + " " });
					AppendBlocks(target, item.Blocks);
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
					Bold bold = new();
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

	private static Brush? GetBrush(string key) =>
		Application.Current.Resources.TryGetValue(key, out var value) ? value as Brush : null;
}
