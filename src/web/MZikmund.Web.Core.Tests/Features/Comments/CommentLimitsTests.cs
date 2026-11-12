using MZikmund.DataContracts.Blog.Comments;

namespace MZikmund.Web.Core.Tests.Features.Comments;

public class CommentLimitsTests
{
	[Fact]
	public void MaxCommentLength_IsFourThousand()
	{
		// Arrange & Act
		var maxCommentLength = CommentLimits.MaxCommentLength;

		// Assert
		Assert.Equal(4000, maxCommentLength);
	}

	[Fact]
	public void MaxReportNoteLength_IsFiveHundred()
	{
		// Arrange & Act
		var maxReportNoteLength = CommentLimits.MaxReportNoteLength;

		// Assert
		Assert.Equal(500, maxReportNoteLength);
	}
}
