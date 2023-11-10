using Moq;
using MZikmund.Web.Core.Services;

namespace MZikmund.Web.Core.Tests.Services;

public class MediaBlobNameGeneratorTests
{
	[Fact]
	public void GeneratedFileNameHasFolder()
	{
		var utcDate = new DateTimeOffset(2003, 2, 18, 0, 0, 0, TimeSpan.Zero);
		var dateProviderMock = new Mock<IDateProvider>();
		dateProviderMock.Setup(d => d.UtcNow).Returns(utcDate);
		var generator = new MediaBlobPathGenerator(dateProviderMock.Object);

		var name = generator.GenerateBlobPath("test.png");
		Assert.Equal("2003/02/18/test.png", name);
	}
}
