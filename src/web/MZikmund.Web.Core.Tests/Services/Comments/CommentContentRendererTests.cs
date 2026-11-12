using MZikmund.Web.Core.Services.Comments;

namespace MZikmund.Web.Core.Tests.Services.Comments;

public class CommentContentRendererTests
{
	private readonly CommentContentRenderer _renderer = new();

	[Theory]
	[InlineData("<script>alert(1)</script>", "<p>&lt;script&gt;alert(1)&lt;/script&gt;</p>")]
	[InlineData("<img src=x onerror=alert(1)>", "<p>&lt;img src=x onerror=alert(1)&gt;</p>")]
	[InlineData("<svg/onload=alert(1)>", "<p>&lt;svg/onload=alert(1)&gt;</p>")]
	[InlineData("<a href=\"https://x\" onclick=\"alert(1)\">raw</a>", "<p>&lt;a href=\"https://x\" onclick=\"alert(1)\"&gt;raw&lt;/a&gt;</p>")]
	[InlineData("foo <!-- comment --> bar", "<p>foo &lt;!-- comment --&gt; bar</p>")]
	[InlineData("[x](javascript:alert(1))", "<p>x</p>")]
	[InlineData("[x](JaVaScRiPt:%61lert(1))", "<p>x</p>")]
	[InlineData("[x](data:text/html;base64,PHNjcmlwdD5hbGVydCgxKTwvc2NyaXB0Pg==)", "<p>x</p>")]
	[InlineData("[x](vbscript:msgbox(1))", "<p>x</p>")]
	[InlineData("![image](https://example.com/a.png)", "<p>image</p>")]
	public void RenderHtml_HostileInput_IsInert(string markdown, string expected)
	{
		// Arrange
		// (renderer constructed in the field initializer)

		// Act
		var html = _renderer.RenderHtml(markdown);

		// Assert
		Assert.Equal(expected, html);
	}

	[Fact]
	public void RenderHtml_HttpsLink_StampsNofollowUgcAndTargetBlank()
	{
		// Arrange
		var markdown = "[click](https://ok.example)";

		// Act
		var html = _renderer.RenderHtml(markdown);

		// Assert
		Assert.Contains("rel=\"nofollow ugc noopener noreferrer\"", html);
		Assert.Contains("target=\"_blank\"", html);
		Assert.Contains("href=\"https://ok.example\"", html);
	}

	[Fact]
	public void RenderHtml_JavaScriptAutolink_ProducesNoAnchorAndNoScheme()
	{
		// Arrange
		// <scheme:rest> is a CommonMark autolink and produces an AutolinkInline, not a LinkInline.
		var markdown = "<javascript:alert(1)>";

		// Act
		var html = _renderer.RenderHtml(markdown);

		// Assert
		Assert.DoesNotContain("<a", html);
		Assert.DoesNotContain("href", html);
	}

	[Fact]
	public void RenderHtml_MalformedMarkup_ProducesNoImgTag()
	{
		// Arrange
		var markdown = "<img src=x onerror=alert(1)";

		// Act
		var html = _renderer.RenderHtml(markdown);

		// Assert
		Assert.DoesNotContain("<img", html);
		Assert.Contains("&lt;img", html);
	}

	[Fact]
	public void RenderHtml_InlineFormatting_IsPreserved()
	{
		// Arrange
		var markdown = "Hello **bold** *italic* ~~strike~~ `code`.";

		// Act
		var html = _renderer.RenderHtml(markdown);

		// Assert
		Assert.Equal("<p>Hello <strong>bold</strong> <em>italic</em> <del>strike</del> <code>code</code>.</p>", html);
	}

	[Fact]
	public void RenderHtml_BlockquotesAndLists_ArePreserved()
	{
		// Arrange
		var markdown = "> quote\n\n- a\n- b\n\n1. one";

		// Act
		var html = _renderer.RenderHtml(markdown);

		// Assert
		Assert.Equal(
			"<blockquote>\n<p>quote</p>\n</blockquote>\n<ul>\n<li>a</li>\n<li>b</li>\n</ul>\n<ol>\n<li>one</li>\n</ol>",
			html);
	}

	[Fact]
	public void RenderHtml_FencedCode_DropsAttackerControlledLanguageClass()
	{
		// Arrange
		var markdown = "```csharp\nvar x = 1;\n```";

		// Act
		var html = _renderer.RenderHtml(markdown);

		// Assert
		Assert.Equal("<pre><code>var x = 1;\n</code></pre>", html);
	}

	[Fact]
	public void RenderHtml_Headings_AreNotRendered()
	{
		// Arrange
		var markdown = "# Heading\n## Heading2\nSetext\n======";

		// Act
		var html = _renderer.RenderHtml(markdown);

		// Assert
		Assert.Equal("<p># Heading\n## Heading2\nSetext\n======</p>", html);
	}

	[Fact]
	public void RenderHtml_EmptyInput_ReturnsEmptyString()
	{
		// Arrange
		var markdown = "   ";

		// Act
		var html = _renderer.RenderHtml(markdown);

		// Assert
		Assert.Equal(string.Empty, html);
	}
}
