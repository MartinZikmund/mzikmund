using MZikmund.Web.Core.Services;

namespace MZikmund.Shared.Tests.Blog;

public class PostPreviewTests
{
	private readonly MarkdownConverter _converter = new();

	[Theory]
	[InlineData("abc", 0, 0)]
	[InlineData("abc", 3, 0)]
	[InlineData("a\rb\rc", 4, 2)]
	[InlineData("a\nb\nc", 4, 2)]
	[InlineData("a\r\nb\r\nc", 6, 2)]
	[InlineData("a\r\nb", 2, 0)]
	[InlineData("a\r\nb", 3, 1)]
	[InlineData("a\rb", 99, 1)]
	[InlineData("a\rb", -5, 0)]
	public void LineAtCountsEveryNewlineStyleOnce(string text, int index, int expectedLine) =>
		Assert.Equal(expectedLine, MarkdownSourceLines.LineAt(text, index));

	[Fact]
	public void SourceLinesAreAddedToTopLevelBlocks()
	{
		var html = _converter.ToHtmlWithSourceLines("# Title\r\rFirst paragraph\r\r- one\r- two\r\r> quote");

		Assert.Contains("<h1 id=\"title\" data-line=\"0\">", html);
		Assert.Contains("<p data-line=\"2\">First paragraph</p>", html);
		Assert.Contains("data-line=\"4\"", html);
		Assert.Contains("data-line=\"7\"", html);
	}

	[Fact]
	public void FencedCodeBlocksKeepTheirSourceLine()
	{
		var html = _converter.ToHtmlWithSourceLines("Intro\r\r```csharp\rvar x = 1;\r```");

		Assert.Contains("data-line=\"2\"", html);
	}

	[Fact]
	public void PublishedHtmlHasNoSourceLines() =>
		Assert.DoesNotContain("data-line", _converter.ToHtml("# Title\r\rText"));

	[Fact]
	public async Task PreviewEmbedsGistsWithoutShiftingLaterLines()
	{
		PostContentProcessor processor = new(_converter);

		var html = await processor.ProcessPreviewAsync("https://gist.github.com/someone/abc123\r\rAfter the gist");

		Assert.Contains("<iframe", html);
		Assert.Contains("https://gist.github.com/someone/abc123.js", html);
		Assert.DoesNotContain("<script", html);
		Assert.Contains("<p data-line=\"2\">After the gist</p>", html);
	}
}
