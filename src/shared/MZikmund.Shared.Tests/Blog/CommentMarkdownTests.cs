using MZikmund.Shared.Blog;

namespace MZikmund.Shared.Tests.Blog;

public class CommentMarkdownTests
{
	private static IReadOnlyList<CommentInline> Inlines(string markdown) =>
		Assert.IsType<ParagraphBlock>(Assert.Single(CommentMarkdown.Parse(markdown))).Inlines;

	// Flattens to the visible text, so a test can assert nothing dangerous survives as a link.
	private static string Flatten(IEnumerable<CommentInline> inlines) =>
		string.Concat(inlines.Select(i => i switch
		{
			TextInline t => t.Text,
			CodeInline c => c.Code,
			StrongInline s => Flatten(s.Children),
			EmphasisInline e => Flatten(e.Children),
			StrikeInline s => Flatten(s.Children),
			LinkInline l => Flatten(l.Children),
			_ => string.Empty,
		}));

	private static bool ContainsLink(IEnumerable<CommentInline> inlines) =>
		inlines.Any(i => i switch
		{
			LinkInline => true,
			StrongInline s => ContainsLink(s.Children),
			EmphasisInline e => ContainsLink(e.Children),
			StrikeInline s => ContainsLink(s.Children),
			_ => false,
		});

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void EmptyInputGivesNoBlocks(string? markdown) =>
		Assert.Empty(CommentMarkdown.Parse(markdown));

	[Fact]
	public void BoldItalicStrikeNest()
	{
		var inlines = Inlines("**bold *both*** ~~gone~~");

		var strong = Assert.IsType<StrongInline>(inlines[0]);
		Assert.Contains(strong.Children, c => c is EmphasisInline);
		Assert.Contains(inlines, i => i is StrikeInline);
	}

	[Fact]
	public void InlineCode()
	{
		var code = Assert.IsType<CodeInline>(Assert.Single(Inlines("`x < y`")));
		Assert.Equal("x < y", code.Code);
	}

	[Fact]
	public void FencedCode()
	{
		var block = Assert.IsType<CodeBlock>(Assert.Single(CommentMarkdown.Parse("```cs\nvar a = 1;\n```")));
		Assert.Equal("var a = 1;", block.Code);
	}

	[Fact]
	public void Quote()
	{
		var quote = Assert.IsType<QuoteBlock>(Assert.Single(CommentMarkdown.Parse("> hello")));
		Assert.IsType<ParagraphBlock>(Assert.Single(quote.Blocks));
	}

	[Fact]
	public void ListMarkers()
	{
		var bullets = CommentMarkdown.Parse("- a\n- b").Cast<ListItemBlock>().ToList();
		Assert.Equal(2, bullets.Count);
		Assert.All(bullets, b => Assert.Equal("\u2022", b.Marker));

		var numbers = CommentMarkdown.Parse("1. a\n2. b").Cast<ListItemBlock>().ToList();
		Assert.Equal("1.", numbers[0].Marker);
		Assert.Equal("2.", numbers[1].Marker);
	}

	[Fact]
	public void HttpLinkIsKept()
	{
		var link = Assert.IsType<LinkInline>(Assert.Single(Inlines("[site](https://example.com/a)")));
		Assert.Equal(new Uri("https://example.com/a"), link.Url);
		Assert.Equal("site", Flatten(link.Children));
	}

	[Fact]
	public void AngleBracketAutolinkIsKept()
	{
		var link = Assert.IsType<LinkInline>(Assert.Single(Inlines("<mailto:me@example.com>")));
		Assert.Equal("mailto", link.Url.Scheme);
	}

	[Theory]
	[InlineData("[x](javascript:alert(1))")]
	[InlineData("[x](JaVaScRiPt:alert(1))")]
	[InlineData("[x](data:text/html;base64,AAAA)")]
	[InlineData("[x](file:///c:/windows/system32)")]
	[InlineData("[x](ms-settings:network)")]
	[InlineData("[x](/relative/path)")]
	[InlineData("[x](//evil.example/x)")]
	[InlineData("<javascript:alert(1)>")]
	public void UnsafeLinksBecomeText(string markdown)
	{
		var inlines = Inlines(markdown);

		Assert.False(ContainsLink(inlines));
		Assert.NotEmpty(Flatten(inlines));
	}

	[Fact]
	public void ImageBecomesAltText()
	{
		var inlines = Inlines("![alt text](https://example.com/p.png)");

		Assert.False(ContainsLink(inlines));
		Assert.Equal("alt text", Flatten(inlines));
	}

	[Fact]
	public void ImageInsideLinkKeepsOnlyTheLink()
	{
		var inlines = Inlines("[![alt](https://example.com/p.png)](https://example.com)");

		var link = Assert.IsType<LinkInline>(Assert.Single(inlines));
		Assert.Equal("alt", Flatten(link.Children));
	}

	[Fact]
	public void RawHtmlIsLiteralText()
	{
		var inlines = Inlines("<script>alert(1)</script> and <img src=x onerror=alert(1)>");

		var text = Flatten(inlines);
		Assert.Contains("<script>", text);
		Assert.Contains("<img", text);
		Assert.False(ContainsLink(inlines));
	}

	[Fact]
	public void RawHtmlBlockIsLiteralText()
	{
		var blocks = CommentMarkdown.Parse("<div onclick=\"x()\">\nhi\n</div>");

		Assert.All(blocks, b => Assert.IsNotType<CodeBlock>(b));
		Assert.Contains("<div", Flatten(blocks.OfType<ParagraphBlock>().SelectMany(p => p.Inlines)));
	}

	[Fact]
	public void HeadingsStayPlainParagraphs()
	{
		var inlines = Inlines("# not a heading");

		Assert.Equal("# not a heading", Flatten(inlines));
	}

	[Theory]
	[InlineData("**unclosed")]
	[InlineData("[](")]
	[InlineData("![]()")]
	[InlineData("<<<>>>")]
	[InlineData("\0\u0001 weird \uD800")]
	public void NeverThrows(string markdown) =>
		Assert.NotNull(CommentMarkdown.Parse(markdown));
}
