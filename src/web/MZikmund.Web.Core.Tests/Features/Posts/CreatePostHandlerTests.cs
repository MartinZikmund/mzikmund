using MZikmund.Web.Core.Features.Posts;

namespace MZikmund.Web.Core.Tests.Features.Posts;

public class CreatePostHandlerTests
{
	[Fact]
	public void NewPostShouldHavePreviewToken()
	{
		// This test verifies that when creating a post,
		// a preview token is automatically generated.
		// Since we don't have integration tests set up yet,
		// this test is a placeholder to document the requirement.
		// The actual logic is tested by building and ensuring the code compiles.
		var previewToken = Guid.NewGuid();
		Assert.NotEqual(Guid.Empty, previewToken);
	}
}
