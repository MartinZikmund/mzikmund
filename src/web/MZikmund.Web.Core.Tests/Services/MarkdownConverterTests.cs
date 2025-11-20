using MZikmund.Web.Core.Services;

namespace MZikmund.Web.Core.Tests.Services;

public class MarkdownConverterTests
{
	[Fact]
	public void ToHtml_AddsSourceLineAttributes()
	{
		// Arrange
		var converter = new MarkdownConverter();
		var markdown = @"# Heading 1

This is a paragraph.

## Heading 2

Another paragraph.";

		// Act
		var html = converter.ToHtml(markdown);

		// Assert
		Assert.Contains("data-source-line", html);
	}

	[Fact]
	public void ToHtml_SourceLineAttributesHaveCorrectLineNumbers()
	{
		// Arrange
		var converter = new MarkdownConverter();
		var markdown = @"# First Line
Second Line
Third Line";

		// Act
		var html = converter.ToHtml(markdown);

		// Assert
		// First element should have data-source-line="0"
		Assert.Contains("data-source-line=\"0\"", html);
	}

	[Fact]
	public void ToHtml_MultipleBlocksHaveDifferentLineNumbers()
	{
		// Arrange
		var converter = new MarkdownConverter();
		var markdown = @"# Heading

Paragraph 1

Paragraph 2";

		// Act
		var html = converter.ToHtml(markdown);

		// Assert
		// Should have multiple data-source-line attributes with different values
		Assert.Contains("data-source-line=\"0\"", html);
		Assert.Contains("data-source-line=\"2\"", html);
		Assert.Contains("data-source-line=\"4\"", html);
	}
}
