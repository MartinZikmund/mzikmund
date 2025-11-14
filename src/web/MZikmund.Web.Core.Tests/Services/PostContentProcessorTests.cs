using Moq;
using MZikmund.Web.Core.Services;

namespace MZikmund.Web.Core.Tests.Services;

public class PostContentProcessorTests
{
	[Fact]
	public async Task ProcessAsync_WithoutCaretPosition_ReturnsConvertedHtml()
	{
		// Arrange
		var markdownConverterMock = new Mock<IMarkdownConverter>();
		markdownConverterMock.Setup(m => m.ToHtml(It.IsAny<string>()))
			.Returns<string>(input => $"<p>{input}</p>");
		var processor = new PostContentProcessor(markdownConverterMock.Object);
		var content = "Hello World";

		// Act
		var result = await processor.ProcessAsync(content);

		// Assert
		Assert.Equal("<p>Hello World</p>", result);
	}

	[Fact]
	public async Task ProcessAsync_WithCaretPositionZero_DoesNotInjectMarker()
	{
		// Arrange
		var markdownConverterMock = new Mock<IMarkdownConverter>();
		markdownConverterMock.Setup(m => m.ToHtml(It.IsAny<string>()))
			.Returns<string>(input => $"<p>{input}</p>");
		var processor = new PostContentProcessor(markdownConverterMock.Object);
		var content = "Hello World";

		// Act
		var result = await processor.ProcessAsync(content, 0);

		// Assert
		Assert.Equal("<p>Hello World</p>", result);
		Assert.DoesNotContain("caret-marker", result);
	}

	[Fact]
	public async Task ProcessAsync_WithValidCaretPosition_InjectsMarker()
	{
		// Arrange
		var markdownConverterMock = new Mock<IMarkdownConverter>();
		markdownConverterMock.Setup(m => m.ToHtml(It.IsAny<string>()))
			.Returns<string>(input => $"<p>{input}</p>");
		var processor = new PostContentProcessor(markdownConverterMock.Object);
		var content = "Hello World";

		// Act
		var result = await processor.ProcessAsync(content, 6); // Position after "Hello "

		// Assert
		Assert.Contains("caret-marker", result);
	}

	[Fact]
	public async Task ProcessAsync_WithCaretPositionBeyondLength_DoesNotInjectMarker()
	{
		// Arrange
		var markdownConverterMock = new Mock<IMarkdownConverter>();
		markdownConverterMock.Setup(m => m.ToHtml(It.IsAny<string>()))
			.Returns<string>(input => $"<p>{input}</p>");
		var processor = new PostContentProcessor(markdownConverterMock.Object);
		var content = "Hello World";

		// Act
		var result = await processor.ProcessAsync(content, 100);

		// Assert
		Assert.DoesNotContain("caret-marker", result);
	}

	[Fact]
	public async Task ProcessAsync_ProcessesGistUrls()
	{
		// Arrange
		var markdownConverterMock = new Mock<IMarkdownConverter>();
		markdownConverterMock.Setup(m => m.ToHtml(It.IsAny<string>()))
			.Returns<string>(input => input); // Pass through for this test
		var processor = new PostContentProcessor(markdownConverterMock.Object);
		var content = "Check out https://gist.github.com/user/abc123";

		// Act
		var result = await processor.ProcessAsync(content);

		// Assert
		Assert.Contains("<script src=\"https://gist.github.com/user/abc123.js\"></script>", result);
	}
}
