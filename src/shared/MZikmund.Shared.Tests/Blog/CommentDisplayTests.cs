using MZikmund.Shared.Blog;

namespace MZikmund.Shared.Tests.Blog;

public class CommentDisplayTests
{
	[Theory]
	[InlineData(null, "")]
	[InlineData("", "")]
	[InlineData("  ", "")]
	[InlineData("Ana", "@Ana ")]
	public void MentionPrefix(string? name, string expected) =>
		Assert.Equal(expected, CommentDisplay.MentionPrefix(name));

	[Theory]
	[InlineData(10, 3, 7)]
	[InlineData(3, 3, 0)]
	[InlineData(2, 3, 0)]
	public void RemainingReplyCount(int total, int shown, int expected) =>
		Assert.Equal(expected, CommentDisplay.RemainingReplyCount(total, shown));

	[Theory]
	[InlineData(-1, "")]
	[InlineData(0, "")]
	[InlineData(1, "1")]
	[InlineData(99, "99")]
	[InlineData(100, "99+")]
	public void FormatBadgeCount(int count, string expected) =>
		Assert.Equal(expected, CommentDisplay.FormatBadgeCount(count));
}
