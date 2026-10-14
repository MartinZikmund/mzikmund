using MZikmund.Shared.Blog;

namespace MZikmund.Shared.Tests.Blog;

public class PostRouteNamesTests
{
	[Theory]
	[InlineData("", "", true)]
	[InlineData("hello-world", "Hello world", true)]
	[InlineData("hello-wor", "Hello wor", true)]
	[InlineData("my-custom-route", "Hello world", false)]
	public void FollowsTitle(string route, string previousTitle, bool expected) =>
		Assert.Equal(expected, PostRouteNames.FollowsTitle(route, previousTitle));
}
