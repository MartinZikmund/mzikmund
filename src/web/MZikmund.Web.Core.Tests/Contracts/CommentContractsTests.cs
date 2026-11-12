using System.Runtime.CompilerServices;
using MZikmund.DataContracts.Blog.Comments;

namespace MZikmund.Web.Core.Tests.Contracts;

public class CommentContractsTests
{
	private const string ContractNamespace = "MZikmund.DataContracts.Blog.Comments";

	private static CommentDto CreateDto() =>
		new()
		{
			Id = new Guid("11111111-1111-1111-1111-111111111111"),
			PostId = new Guid("22222222-2222-2222-2222-222222222222"),
			AuthorDisplayName = "Martin",
			Content = "Hello **world**",
			ContentHtml = "<p>Hello <strong>world</strong></p>",
			Status = CommentStatus.Visible,
			CreatedOn = new DateTime(2026, 8, 11, 10, 0, 0, DateTimeKind.Utc),
			UpvoteCount = 3,
			ReplyCount = 0,
			HasUpvoted = false,
			IsOwn = false,
		};

	[Fact]
	public void CommentStatus_Members_MatchTheStoredOrdinals()
	{
		// Arrange
		var visible = CommentStatus.Visible;
		var deletedByAuthor = CommentStatus.DeletedByAuthor;
		var hiddenByModerator = CommentStatus.HiddenByModerator;

		// Act
		var ordinals = new[] { (int)visible, (int)deletedByAuthor, (int)hiddenByModerator };

		// Assert
		Assert.Equal(0, ordinals[0]);
		Assert.Equal(1, ordinals[1]);
		Assert.Equal(2, ordinals[2]);
	}

	[Fact]
	public void CommentReportReason_Members_MatchTheStoredOrdinals()
	{
		// Arrange
		var expected = new[] { "Spam", "Abuse", "OffTopic", "Other" };

		// Act
		var names = Enum.GetNames<CommentReportReason>();

		// Assert
		Assert.Equal(expected, names);
		Assert.Equal(0, (int)CommentReportReason.Spam);
		Assert.Equal(3, (int)CommentReportReason.Other);
	}

	[Fact]
	public void CommentSortOrder_Members_AreTopAndNew()
	{
		// Arrange
		var expected = new[] { "Top", "New" };

		// Act
		var names = Enum.GetNames<CommentSortOrder>();

		// Assert
		Assert.Equal(expected, names);
		Assert.Equal(0, (int)CommentSortOrder.Top);
		Assert.Equal(1, (int)CommentSortOrder.New);
	}

	[Fact]
	public void CommentDto_Replies_DefaultsToAnEmptyArray()
	{
		// Arrange
		var dto = CreateDto();

		// Act
		var replies = dto.Replies;

		// Assert
		Assert.NotNull(replies);
		Assert.Empty(replies);
	}

	[Fact]
	public void CommentDto_CoreMembers_AreDeclaredRequired()
	{
		// Arrange
		var type = typeof(CommentDto);
		var expectedRequired = new[]
		{
			nameof(CommentDto.Id),
			nameof(CommentDto.PostId),
			nameof(CommentDto.AuthorDisplayName),
			nameof(CommentDto.Content),
			nameof(CommentDto.ContentHtml),
			nameof(CommentDto.Status),
			nameof(CommentDto.CreatedOn),
			nameof(CommentDto.UpvoteCount),
			nameof(CommentDto.ReplyCount),
			nameof(CommentDto.HasUpvoted),
			nameof(CommentDto.IsOwn),
		};

		// Act
		var missing = expectedRequired
			.Where(name => !type.GetProperty(name)!
				.GetCustomAttributes(inherit: false)
				.Any(attribute => attribute is RequiredMemberAttribute))
			.ToArray();

		// Assert
		Assert.Empty(missing);
	}

	[Fact]
	public void CreateCommentRequest_WithTheSameValues_AreEqual()
	{
		// Arrange
		var parentId = new Guid("33333333-3333-3333-3333-333333333333");

		// Act
		CreateCommentRequest first = new() { Content = "Hi", ParentId = parentId };
		CreateCommentRequest second = new() { Content = "Hi", ParentId = parentId };

		// Assert
		Assert.Equal(first, second);
		Assert.NotSame(first, second);
	}

	[Fact]
	public void RequestContracts_ExposeTheWriteSurface()
	{
		// Arrange
		var note = "Not about the post";

		// Act
		UpdateCommentRequest update = new() { Content = "Edited" };
		ReportCommentRequest report = new() { Reason = CommentReportReason.OffTopic, Note = note };

		// Assert
		Assert.Equal("Edited", update.Content);
		Assert.Equal(CommentReportReason.OffTopic, report.Reason);
		Assert.Equal(note, report.Note);
	}

	[Fact]
	public void CommentReportDto_CarriesTheModerationQueueShape()
	{
		// Arrange
		var comment = CreateDto();
		var reasons = new[] { CommentReportReason.Spam, CommentReportReason.Abuse };
		var firstReportedOn = new DateTime(2026, 8, 11, 9, 0, 0, DateTimeKind.Utc);
		var authorId = new Guid("55555555-5555-5555-5555-555555555555");

		// Act
		CommentReportDto report = new()
		{
			CommentId = comment.Id,
			AuthorId = authorId,
			Comment = comment,
			OpenReportCount = 2,
			Reasons = reasons,
			FirstReportedOn = firstReportedOn,
		};

		// Assert
		Assert.Equal(comment.Id, report.CommentId);
		Assert.Equal(authorId, report.AuthorId);
		Assert.Same(comment, report.Comment);
		Assert.Equal(2, report.OpenReportCount);
		Assert.Equal(reasons, report.Reasons);
		Assert.Equal(firstReportedOn, report.FirstReportedOn);
	}

	[Fact]
	public void Contracts_DeclareExactlyTheElevenCanonicalTypes()
	{
		// Arrange
		var expected = new[]
		{
			"CommentDto",
			"CommentErrorCodes",
			"CommentLimits",
			"CommentModerationDto",
			"CommentReportDto",
			"CommentReportReason",
			"CommentSortOrder",
			"CommentStatus",
			"CreateCommentRequest",
			"ReportCommentRequest",
			"UpdateCommentRequest",
		};

		// Act
		var names = typeof(CommentDto).Assembly
			.GetTypes()
			.Where(type => type.Namespace == ContractNamespace)
			.Select(type => type.Name)
			.OrderBy(name => name, StringComparer.Ordinal)
			.ToArray();

		// Assert
		Assert.Equal(expected, names);
	}

	[Fact]
	public void Contracts_CarryNoSystemTextJsonAttributes()
	{
		// Arrange
		var contractTypes = typeof(CommentDto).Assembly
			.GetTypes()
			.Where(type => type.Namespace == ContractNamespace)
			.ToArray();

		// Act
		var jsonAttributes = contractTypes
			.SelectMany(type => type.GetCustomAttributes(inherit: false)
				.Concat(type.GetProperties().SelectMany(property => property.GetCustomAttributes(inherit: false))))
			.Where(attribute => attribute.GetType().Namespace == "System.Text.Json.Serialization")
			.ToArray();

		// Assert
		Assert.Empty(jsonAttributes);
	}
}
