using MZikmund.Web.Core.Features.Comments;
using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Extensions;
using static MZikmund.Web.Core.Tests.Features.Comments.CommentSeed;

namespace MZikmund.Web.Core.Tests.Features.Comments;

public class CommentPostVisibilityTests
{
	public static TheoryData<PostStatus, int?> Cases()
	{
		TheoryData<PostStatus, int?> data = new();
		foreach (var status in Enum.GetValues<PostStatus>())
		{
			// null: never published; -1 / 0 / 1: published a day before, exactly at, a day after "now".
			foreach (int? offsetDays in new int?[] { null, -1, 0, 1 })
			{
				data.Add(status, offsetDays);
			}
		}

		return data;
	}

	[Theory]
	[MemberData(nameof(Cases))]
	public void PublicRule_AgreesWithTheRuleTheSharedPostQueriesUse(PostStatus status, int? offsetDays)
	{
		// Arrange
		PostEntity post = new()
		{
			Status = status,
			PublishedDate = offsetDays is null ? null : Now.AddDays(offsetDays.Value),
		};
		var shared = PostEntityExtensions.IsPublishedAndVisible(Now).Compile();
		var local = CommentPostVisibility.PublicRule.Compile();

		// Act & Assert
		Assert.Equal(shared(post), local(post, Now));
	}
}
