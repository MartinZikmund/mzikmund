using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Web.Core.Mappings;
using MZikmund.Web.Data.Entities;
using DtoStatus = MZikmund.DataContracts.Blog.Comments.CommentStatus;
using EntityStatus = MZikmund.Web.Data.Entities.CommentStatus;

namespace MZikmund.Web.Core.Tests.Mappings;

public class CommentMapTests
{
	private static MapperConfiguration CreateConfiguration() =>
		new(cfg => cfg.AddProfile<CommentMap>(), NullLoggerFactory.Instance);

	private static CommentEntity CreateEntity() =>
		new()
		{
			Id = new Guid("11111111-1111-1111-1111-111111111111"),
			PostId = new Guid("22222222-2222-2222-2222-222222222222"),
			ParentId = new Guid("33333333-3333-3333-3333-333333333333"),
			AuthorId = new Guid("44444444-4444-4444-4444-444444444444"),
			Content = "Hello **world**",
			ContentHtml = "<p>Hello <strong>world</strong></p>",
			MentionedDisplayName = "Jane",
			Status = EntityStatus.HiddenByModerator,
			CreatedOn = new DateTime(2026, 8, 11, 10, 0, 0, DateTimeKind.Utc),
			EditedOn = new DateTime(2026, 8, 11, 10, 5, 0, DateTimeKind.Utc),
			UpvoteCount = 7,
			ReplyCount = 2,
			ReportCount = 1,
			Author = new CommentAuthorEntity
			{
				Id = new Guid("44444444-4444-4444-4444-444444444444"),
				DisplayName = "Martin",
				AvatarUrl = "https://example.com/avatar.png",
				Email = "martin@example.com",
			},
		};

	[Fact]
	public void Configuration_CommentProfile_IsValid()
	{
		// Arrange
		var configuration = CreateConfiguration();

		// Act
		var exception = Record.Exception(configuration.AssertConfigurationIsValid);

		// Assert
		Assert.Null(exception);
	}

	[Fact]
	public void Map_CommentEntity_CopiesScalarMembers()
	{
		// Arrange
		var mapper = CreateConfiguration().CreateMapper();
		var entity = CreateEntity();

		// Act
		var dto = mapper.Map<CommentDto>(entity);

		// Assert
		Assert.Equal(entity.Id, dto.Id);
		Assert.Equal(entity.PostId, dto.PostId);
		Assert.Equal(entity.ParentId, dto.ParentId);
		Assert.Equal("Hello **world**", dto.Content);
		Assert.Equal("<p>Hello <strong>world</strong></p>", dto.ContentHtml);
		Assert.Equal("Jane", dto.MentionedDisplayName);
		Assert.Equal(DtoStatus.HiddenByModerator, dto.Status);
		Assert.Equal(entity.CreatedOn, dto.CreatedOn);
		Assert.Equal(entity.EditedOn, dto.EditedOn);
		Assert.Equal(7, dto.UpvoteCount);
		Assert.Equal(2, dto.ReplyCount);
	}

	[Fact]
	public void Map_CommentEntity_FlattensAuthorDetails()
	{
		// Arrange
		var mapper = CreateConfiguration().CreateMapper();
		var entity = CreateEntity();

		// Act
		var dto = mapper.Map<CommentDto>(entity);

		// Assert
		Assert.Equal("Martin", dto.AuthorDisplayName);
		Assert.Equal("https://example.com/avatar.png", dto.AuthorAvatarUrl);
	}

	[Fact]
	public void Map_CommentEntity_LeavesCallerSpecificMembersAtDefaults()
	{
		// Arrange
		var mapper = CreateConfiguration().CreateMapper();
		var entity = CreateEntity();

		// Act
		var dto = mapper.Map<CommentDto>(entity);

		// Assert
		Assert.False(dto.HasUpvoted);
		Assert.False(dto.IsOwn);
		Assert.NotNull(dto.Replies);
		Assert.Empty(dto.Replies);
	}
}
