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
}
