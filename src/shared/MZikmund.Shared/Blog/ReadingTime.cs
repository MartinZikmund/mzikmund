using System.Text.RegularExpressions;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using MarkdigCodeBlock = Markdig.Syntax.CodeBlock;
using MarkdigCodeInline = Markdig.Syntax.Inlines.CodeInline;
using MarkdigLinkInline = Markdig.Syntax.Inlines.LinkInline;

namespace MZikmund.Shared.Blog;

public static partial class ReadingTime
{
	/// <summary>
	/// Average silent reading speed of adults for non-fiction (Brysbaert, 2019).
	/// </summary>
	public const int WordsPerMinute = 238;

	private const double SecondsPerImage = 12;

	// Code is skimmed rather than read word by word, and gists don't even live in the content.
	private const double SecondsPerCodeSample = 20;

	private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

	/// <summary>
	/// Whole minutes needed to read a post, rounded up; 0 when there is nothing to read.
	/// </summary>
	public static int EstimateMinutes(string? markdown)
	{
		if (string.IsNullOrWhiteSpace(markdown))
		{
			return 0;
		}

		var codeSamples = GistUrlRegex().Count(markdown);
		var document = Markdig.Markdown.Parse(GistUrlRegex().Replace(markdown, ""), Pipeline);

		var words = 0;
		foreach (var literal in document.Descendants<LiteralInline>())
		{
			if (!IsInsideImage(literal))
			{
				words += CountWords(literal.Content.AsSpan());
			}
		}

		foreach (var code in document.Descendants<MarkdigCodeInline>())
		{
			words += CountWords(code.Content);
		}

		var images = document.Descendants<MarkdigLinkInline>().Count(link => link.IsImage);
		codeSamples += document.Descendants<MarkdigCodeBlock>().Count();

		var seconds = words * 60.0 / WordsPerMinute + images * SecondsPerImage + codeSamples * SecondsPerCodeSample;
		return (int)Math.Ceiling(seconds / 60);
	}

	private static bool IsInsideImage(Inline inline)
	{
		for (var parent = inline.Parent; parent is not null; parent = parent.Parent)
		{
			if (parent is MarkdigLinkInline { IsImage: true })
			{
				return true;
			}
		}

		return false;
	}

	private static int CountWords(ReadOnlySpan<char> text)
	{
		var count = 0;
		var inWord = false;
		foreach (var c in text)
		{
			var isWordChar = !char.IsWhiteSpace(c);
			if (isWordChar && !inWord)
			{
				count++;
			}

			inWord = isWordChar;
		}

		return count;
	}

	[GeneratedRegex(@"https://gist\.github\.com/[\w-]+/[\w-]+")]
	private static partial Regex GistUrlRegex();
}
