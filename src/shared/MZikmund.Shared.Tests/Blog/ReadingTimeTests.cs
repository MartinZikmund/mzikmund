using MZikmund.Shared.Blog;

namespace MZikmund.Shared.Tests.Blog;

public class ReadingTimeTests
{
	private static string Words(int count) => string.Join(' ', Enumerable.Repeat("word", count));

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("  \r\n ")]
	public void EmptyContentHasNoReadingTime(string? markdown) =>
		Assert.Equal(0, ReadingTime.EstimateMinutes(markdown));

	[Theory]
	[InlineData(1, 1)]
	[InlineData(238, 1)]
	[InlineData(239, 2)]
	[InlineData(476, 2)]
	[InlineData(2380, 10)]
	public void RoundsUpToWholeMinutes(int words, int expectedMinutes) =>
		Assert.Equal(expectedMinutes, ReadingTime.EstimateMinutes(Words(words)));

	[Fact]
	public void MarkupAndLinkTargetsAreNotWords() =>
		Assert.Equal(1, ReadingTime.EstimateMinutes(
			$"# {Words(100)}\r\n\r\n**{Words(100)}** [{Words(38)}](https://example.com/a/very/long/url/that/is/not/read)"));

	[Fact]
	public void TableCellsAndInlineCodeAreRead() =>
		Assert.Equal(2, ReadingTime.EstimateMinutes($"| a | b |\r\n|---|---|\r\n| {Words(200)} | `{Words(39)}` |"));

	[Fact]
	public void EachImageAddsTwelveSeconds()
	{
		// 230 words take 58 s; one image pushes it past a minute, its alt text does not count.
		var text = Words(230);

		Assert.Equal(1, ReadingTime.EstimateMinutes(text));
		Assert.Equal(2, ReadingTime.EstimateMinutes($"{text}\r\n\r\n![{Words(50)}](https://cdn.example/a.png)"));
	}

	[Fact]
	public void GistsAndCodeBlocksCountAsTwentySecondsEach()
	{
		// 200 words take 50.4 s; one embed adds 20 s, which tips it into a second minute. Code words do not count.
		var text = Words(200);

		Assert.Equal(1, ReadingTime.EstimateMinutes(text));
		Assert.Equal(2, ReadingTime.EstimateMinutes($"{text}\r\n\r\nhttps://gist.github.com/someone/abc123"));
		Assert.Equal(2, ReadingTime.EstimateMinutes($"{text}\r\n\r\n```csharp\r\n{Words(500)}\r\n```"));
	}

	[Fact]
	public void AGistAloneIsStillWorthAMinute() =>
		Assert.Equal(1, ReadingTime.EstimateMinutes("https://gist.github.com/someone/abc123"));
}
