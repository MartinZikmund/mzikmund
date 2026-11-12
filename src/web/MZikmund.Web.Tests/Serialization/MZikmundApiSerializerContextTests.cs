using System.Text.Json;
using MZikmund.Api.Serialization;
using MZikmund.DataContracts;
using MZikmund.DataContracts.Blog.Comments;
using Refit;

namespace MZikmund.Web.Tests.Serialization;

public class MZikmundApiSerializerContextTests
{
	[Theory]
	[InlineData(typeof(CommentDto))]
	[InlineData(typeof(CommentDto[]))]
	[InlineData(typeof(IEnumerable<CommentDto>))]
	[InlineData(typeof(PagedResponse<CommentDto>))]
	[InlineData(typeof(ApiResponse<CommentDto>))]
	[InlineData(typeof(ApiResponse<PagedResponse<CommentDto>>))]
	[InlineData(typeof(CreateCommentRequest))]
	[InlineData(typeof(UpdateCommentRequest))]
	[InlineData(typeof(ReportCommentRequest))]
	[InlineData(typeof(CommentModerationDto))]
	[InlineData(typeof(CommentModerationDto[]))]
	[InlineData(typeof(IEnumerable<CommentModerationDto>))]
	[InlineData(typeof(PagedResponse<CommentModerationDto>))]
	[InlineData(typeof(ApiResponse<PagedResponse<CommentModerationDto>>))]
	[InlineData(typeof(CommentReportDto))]
	[InlineData(typeof(CommentReportDto[]))]
	[InlineData(typeof(IEnumerable<CommentReportDto>))]
	[InlineData(typeof(PagedResponse<CommentReportDto>))]
	[InlineData(typeof(ApiResponse<PagedResponse<CommentReportDto>>))]
	[InlineData(typeof(CommentStatus))]
	[InlineData(typeof(CommentReportReason))]
	[InlineData(typeof(CommentSortOrder))]
	[InlineData(typeof(int))]
	[InlineData(typeof(ApiResponse<int>))]
	[InlineData(typeof(ApiResponse<object?>))]
	public void Default_HasTypeInfoForEveryCommentTypeCrossingRefit(Type type)
	{
		// Arrange
		var context = MZikmundApiSerializerContext.Default;

		// Act
		var typeInfo = context.GetTypeInfo(type);

		// Assert
		Assert.NotNull(typeInfo);
	}

	[Fact]
	public void CommentModerationDto_RoundTripsWithItsAuthorId()
	{
		// Arrange
		var authorId = Guid.NewGuid();
		CommentModerationDto dto = new(CreateDto(), authorId);

		// Act
		var json = JsonSerializer.Serialize(dto, MZikmundApiSerializerContext.Default.CommentModerationDto);
		var roundTripped = JsonSerializer.Deserialize(json, MZikmundApiSerializerContext.Default.CommentModerationDto);

		// Assert
		Assert.Equal(authorId, roundTripped!.AuthorId);
		Assert.Equal("Martin", roundTripped.AuthorDisplayName);
	}

	[Fact]
	public void CommentDto_NeverCarriesAnAuthorId()
	{
		// Arrange
		var dto = CreateDto();

		// Act
		var json = JsonSerializer.Serialize(dto, MZikmundApiSerializerContext.Default.CommentDto);

		// Assert
		Assert.DoesNotContain("authorId", json, StringComparison.OrdinalIgnoreCase);
	}

	private static CommentDto CreateDto() => new()
	{
		Id = Guid.NewGuid(),
		PostId = Guid.NewGuid(),
		AuthorDisplayName = "Martin",
		AuthorIsAdmin = true,
		Content = "Hi",
		ContentHtml = "<p>Hi</p>",
		Status = CommentStatus.Visible,
		CreatedOn = new DateTime(2026, 8, 11, 10, 0, 0, DateTimeKind.Utc),
		UpvoteCount = 0,
		ReplyCount = 0,
		HasUpvoted = false,
		IsOwn = false,
	};
}
