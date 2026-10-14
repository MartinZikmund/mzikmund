using Markdig;
using Markdig.Extensions.EmphasisExtras;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Md = Markdig.Syntax;

namespace MZikmund.Shared.Blog;

public abstract record CommentBlock;

public sealed record ParagraphBlock(IReadOnlyList<CommentInline> Inlines) : CommentBlock;

public sealed record CodeBlock(string Code) : CommentBlock;

public sealed record QuoteBlock(IReadOnlyList<CommentBlock> Blocks) : CommentBlock;

public sealed record ListItemBlock(string Marker, IReadOnlyList<CommentBlock> Blocks) : CommentBlock;

public abstract record CommentInline;

public sealed record TextInline(string Text) : CommentInline;

public sealed record StrongInline(IReadOnlyList<CommentInline> Children) : CommentInline;

public sealed record EmphasisInline(IReadOnlyList<CommentInline> Children) : CommentInline;

public sealed record StrikeInline(IReadOnlyList<CommentInline> Children) : CommentInline;

public sealed record CodeInline(string Code) : CommentInline;

public sealed record LinkInline(Uri Url, IReadOnlyList<CommentInline> Children) : CommentInline;

public sealed record LineBreakInline : CommentInline;

/// <summary>
/// Parses comment markdown into a small model the app renders as native inlines.
/// Mirrors the server's restricted renderer: no raw HTML, no images, no headings, and links
/// only for absolute http, https and mailto URLs. Everything else degrades to literal text.
/// </summary>
public static class CommentMarkdown
{
	private static readonly string[] AllowedSchemes = ["http", "https", "mailto"];

	private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
		.DisableHtml()
		.DisableHeadings()
		.UseEmphasisExtras(EmphasisExtraOptions.Strikethrough)
		.Build();

	public static IReadOnlyList<CommentBlock> Parse(string? markdown)
	{
		if (string.IsNullOrWhiteSpace(markdown))
		{
			return Array.Empty<CommentBlock>();
		}

		try
		{
			var document = Markdig.Markdown.Parse(markdown, Pipeline);
			return ConvertBlocks(document);
		}
		catch (Exception)
		{
			return new CommentBlock[] { new ParagraphBlock(new CommentInline[] { new TextInline(markdown) }) };
		}
	}

	private static List<CommentBlock> ConvertBlocks(IEnumerable<Block> blocks)
	{
		List<CommentBlock> result = new();
		foreach (var block in blocks)
		{
			switch (block)
			{
				case LeafBlock { Inline: not null } leaf when block is Md.ParagraphBlock or Md.HeadingBlock:
					result.Add(new ParagraphBlock(ConvertInlines(leaf.Inline!)));
					break;

				case Md.CodeBlock code:
					result.Add(new CodeBlock(code.Lines.ToString().TrimEnd('\r', '\n')));
					break;

				case Md.QuoteBlock quote:
					result.Add(new QuoteBlock(ConvertBlocks(quote)));
					break;

				case Md.ListBlock list:
					var index = int.TryParse(list.OrderedStart, out var start) ? start : 1;
					foreach (var item in list.OfType<Md.ListItemBlock>())
					{
						var marker = list.IsOrdered ? $"{index++}{list.OrderedDelimiter}" : "\u2022";
						result.Add(new ListItemBlock(marker, ConvertBlocks(item)));
					}

					break;

				case Md.ThematicBreakBlock:
					result.Add(new ParagraphBlock(new CommentInline[] { new TextInline("\u2014\u2014\u2014") }));
					break;

				case LeafBlock leaf:
					var text = leaf.Lines.ToString();
					if (!string.IsNullOrWhiteSpace(text))
					{
						result.Add(new ParagraphBlock(new CommentInline[] { new TextInline(text) }));
					}

					break;

				case ContainerBlock container:
					result.AddRange(ConvertBlocks(container));
					break;
			}
		}

		return result;
	}

	private static List<CommentInline> ConvertInlines(ContainerInline container)
	{
		List<CommentInline> result = new();
		foreach (var inline in container)
		{
			switch (inline)
			{
				case LiteralInline literal:
					result.Add(new TextInline(literal.Content.ToString()));
					break;

				case HtmlEntityInline entity:
					result.Add(new TextInline(entity.Transcoded.ToString()));
					break;

				case Md.Inlines.CodeInline code:
					result.Add(new CodeInline(code.Content));
					break;

				case Md.Inlines.LineBreakInline:
					result.Add(new LineBreakInline());
					break;

				case Md.Inlines.EmphasisInline emphasis:
					var children = ConvertInlines(emphasis);
					result.Add(emphasis.DelimiterChar switch
					{
						'~' => new StrikeInline(children),
						_ when emphasis.DelimiterCount >= 2 => new StrongInline(children),
						_ => new EmphasisInline(children),
					});
					break;

				case Md.Inlines.LinkInline { IsImage: true } image:
					result.AddRange(ConvertInlines(image));
					break;

				case Md.Inlines.LinkInline link:
					var linkChildren = ConvertInlines(link);
					if (TryGetAllowedUri(link.Url, out var uri))
					{
						result.Add(new LinkInline(uri, linkChildren));
					}
					else
					{
						result.AddRange(linkChildren);
					}

					break;

				case AutolinkInline autolink:
					if (TryGetAllowedUri(autolink.Url, out var autoUri))
					{
						result.Add(new LinkInline(autoUri, new CommentInline[] { new TextInline(autolink.Url) }));
					}
					else
					{
						result.Add(new TextInline(autolink.Url));
					}

					break;

				case ContainerInline nested:
					result.AddRange(ConvertInlines(nested));
					break;

				default:
					break;
			}
		}

		return result;
	}

	private static bool TryGetAllowedUri(string? url, out Uri uri)
	{
		if (!string.IsNullOrWhiteSpace(url)
			&& Uri.TryCreate(url, UriKind.Absolute, out var parsed)
			&& AllowedSchemes.Contains(parsed.Scheme, StringComparer.OrdinalIgnoreCase))
		{
			uri = parsed;
			return true;
		}

		uri = null!;
		return false;
	}
}
