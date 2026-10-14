using Microsoft.EntityFrameworkCore;
using Moq;
using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Web.Core.Features.Comments;
using MZikmund.Web.Core.Services.Comments;
using MZikmund.Web.Core.Tests.Infrastructure;
using MZikmund.Web.Data;
using MZikmund.Web.Data.Entities;
using static MZikmund.Web.Core.Tests.Features.Comments.CommentSeed;
using DataCommentStatus = MZikmund.Web.Data.Entities.CommentStatus;

namespace MZikmund.Web.Core.Tests.Features.Comments;

public class UpdateCommentHandlerTests
{
	private readonly Mock<ICommentContentRenderer> _mockRenderer;
	private readonly Guid _postId = Guid.NewGuid();
	private readonly Guid _authorId = Guid.NewGuid();
	private readonly Guid _otherAuthorId = Guid.NewGuid();

	public UpdateCommentHandlerTests()
	{
		_mockRenderer = new Mock<ICommentContentRenderer>();
		_mockRenderer
			.Setup(r => r.RenderHtml(It.IsAny<string>()))
			.Returns<string>(markdown => "<p>" + markdown + "</p>");
	}

	[Fact]
	public async Task Handle_WithinFifteenMinuteWindow_UpdatesContentAndRerendersHtml()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		await SeedAsync(db);
		var comment = AddComment(db, _postId, _authorId, createdOn: Now.UtcDateTime.AddMinutes(-5));
		await db.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(db);

		// Act
		var result = await handler.Handle(new UpdateCommentCommand(comment.Id, "  edited body  "), CancellationToken.None);

		// Assert
		var stored = await db.Comment.FirstAsync(c => c.Id == comment.Id, CancellationToken.None);
		Assert.Equal("edited body", stored.Content);
		Assert.Equal("<p>edited body</p>", stored.ContentHtml);
		Assert.Equal(Now.UtcDateTime, stored.EditedOn);
		Assert.Equal("edited body", result.Content);
	}

	[Fact]
	public async Task Handle_AfterFifteenMinuteWindow_ThrowsAndLeavesContentUnchanged()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		await SeedAsync(db);
		var comment = AddComment(db, _postId, _authorId, createdOn: Now.UtcDateTime.AddMinutes(-16));
		await db.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(db);

		// Act & Assert
		await Assert.ThrowsAsync<CommentConflictException>(
			() => handler.Handle(new UpdateCommentCommand(comment.Id, "edited body"), CancellationToken.None));
		var stored = await db.Comment.FirstAsync(c => c.Id == comment.Id, CancellationToken.None);
		Assert.Equal("Existing comment", stored.Content);
		Assert.Null(stored.EditedOn);
	}

	[Fact]
	public async Task Handle_CommentOwnedByAnotherAuthor_Throws()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		await SeedAsync(db);
		var comment = AddComment(db, _postId, _otherAuthorId);
		await db.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(db);

		// Act & Assert
		await Assert.ThrowsAsync<CommentForbiddenException>(
			() => handler.Handle(new UpdateCommentCommand(comment.Id, "edited body"), CancellationToken.None));
		var stored = await db.Comment.FirstAsync(c => c.Id == comment.Id, CancellationToken.None);
		Assert.Equal("Existing comment", stored.Content);
	}

	[Theory]
	[InlineData(DataCommentStatus.HiddenByModerator)]
	[InlineData(DataCommentStatus.DeletedByAuthor)]
	public async Task Handle_NonVisibleComment_ThrowsAndLeavesContentUnchanged(DataCommentStatus status)
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		await SeedAsync(db);
		var comment = AddComment(db, _postId, _authorId, status: status);
		await db.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(db);

		// Act & Assert
		await Assert.ThrowsAsync<CommentConflictException>(
			() => handler.Handle(new UpdateCommentCommand(comment.Id, "edited body"), CancellationToken.None));
		var stored = await db.Comment.FirstAsync(c => c.Id == comment.Id, CancellationToken.None);
		Assert.Equal("Existing comment", stored.Content);
	}

	[Fact]
	public async Task Handle_RepointedReply_LeavesMentionAndParentUnchanged()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		await SeedAsync(db);
		var parent = AddComment(db, _postId, _otherAuthorId);
		var reply = AddComment(db, _postId, _authorId, parentId: parent.Id);
		reply.MentionedDisplayName = "Jane";
		await db.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(db);

		// Act
		await handler.Handle(new UpdateCommentCommand(reply.Id, "edited reply"), CancellationToken.None);

		// Assert
		var stored = await db.Comment.FirstAsync(c => c.Id == reply.Id, CancellationToken.None);
		Assert.Equal("edited reply", stored.Content);
		Assert.Equal("Jane", stored.MentionedDisplayName);
		Assert.Equal(parent.Id, stored.ParentId);
		Assert.DoesNotContain("@Jane", stored.Content);
	}

	[Fact]
	public async Task Handle_ContentOverMaxLength_ThrowsAndLeavesContentUnchanged()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		await SeedAsync(db);
		var comment = AddComment(db, _postId, _authorId);
		await db.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(db);
		var tooLong = new string('a', CommentLimits.MaxCommentLength + 1);

		// Act & Assert
		await Assert.ThrowsAsync<CommentValidationException>(
			() => handler.Handle(new UpdateCommentCommand(comment.Id, tooLong), CancellationToken.None));
		var stored = await db.Comment.FirstAsync(c => c.Id == comment.Id, CancellationToken.None);
		Assert.Equal("Existing comment", stored.Content);
	}

	[Fact]
	public async Task Handle_CommentOnADraftPost_ThrowsNotFoundAndLeavesTheText()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		await SeedAsync(db);
		var comment = AddComment(db, _postId, _authorId, "original", createdOn: Now.UtcDateTime.AddMinutes(-5));
		await db.SaveChangesAsync(CancellationToken.None);
		await db.Post.ExecuteUpdateAsync(
			s => s.SetProperty(p => p.Status, PostStatus.Draft), CancellationToken.None);
		var handler = CreateHandler(db);

		// Act & Assert
		await Assert.ThrowsAsync<CommentNotFoundException>(
			() => handler.Handle(new UpdateCommentCommand(comment.Id, "edited"), CancellationToken.None));
		var stored = await db.Comment.AsNoTracking().FirstAsync(c => c.Id == comment.Id, CancellationToken.None);
		Assert.Equal("original", stored.Content);
	}

	private async Task SeedAsync(DatabaseContext db)
	{
		AddPost(db, _postId);
		AddAuthor(db, _authorId, "Martin");
		AddAuthor(db, _otherAuthorId, "Jane");
		await db.SaveChangesAsync(CancellationToken.None);
	}

	private UpdateCommentHandler CreateHandler(DatabaseContext dbContext) =>
		new(
			dbContext,
			CurrentUser(_authorId, "Martin").Object,
			_mockRenderer.Object,
			FixedClock().Object,
			Logger<UpdateCommentHandler>().Object);
}
