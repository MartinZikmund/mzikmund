using Microsoft.EntityFrameworkCore;
using MZikmund.Web.Core.Features.Comments;
using MZikmund.Web.Core.Services.Comments;
using MZikmund.Web.Core.Tests.Infrastructure;
using MZikmund.Web.Data;
using static MZikmund.Web.Core.Tests.Features.Comments.CommentSeed;
using DataCommentStatus = MZikmund.Web.Data.Entities.CommentStatus;

namespace MZikmund.Web.Core.Tests.Features.Comments;

public class BanAuthorHandlerTests
{
	private readonly Guid _postId = Guid.NewGuid();
	private readonly Guid _authorId = Guid.NewGuid();

	[Fact]
	public async Task Handle_ExistingAuthor_SetsIsBanned()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		AddAuthor(db, _authorId, "Troublemaker");
		await db.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(db);

		// Act
		await handler.Handle(new BanAuthorCommand(_authorId), CancellationToken.None);

		// Assert
		var stored = await db.CommentAuthor.FirstAsync(a => a.Id == _authorId, CancellationToken.None);
		Assert.True(stored.IsBanned);
	}

	[Fact]
	public async Task Handle_BannedAuthor_LeavesExistingCommentsUntouched()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		AddPost(db, _postId);
		AddAuthor(db, _authorId, "Troublemaker");
		var first = AddComment(db, _postId, _authorId, "first", upvoteCount: 4);
		var second = AddComment(db, _postId, _authorId, "second");
		await db.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(db);

		// Act
		await handler.Handle(new BanAuthorCommand(_authorId), CancellationToken.None);

		// Assert
		var comments = await db.Comment.Where(c => c.AuthorId == _authorId).ToListAsync(CancellationToken.None);
		Assert.Equal(2, comments.Count);
		Assert.All(comments, c => Assert.Equal(DataCommentStatus.Visible, c.Status));
		var storedFirst = comments.Single(c => c.Id == first.Id);
		Assert.Equal(4, storedFirst.UpvoteCount);
		Assert.Equal("first", storedFirst.Content);
	}

	[Fact]
	public async Task Handle_AlreadyBannedAuthor_StaysBanned()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		AddAuthor(db, _authorId, "Troublemaker").IsBanned = true;
		await db.SaveChangesAsync(CancellationToken.None);
		var handler = CreateHandler(db);

		// Act
		await handler.Handle(new BanAuthorCommand(_authorId), CancellationToken.None);

		// Assert
		var stored = await db.CommentAuthor.FirstAsync(a => a.Id == _authorId, CancellationToken.None);
		Assert.True(stored.IsBanned);
	}

	[Fact]
	public async Task Handle_UnknownAuthor_ThrowsCommentNotFoundException()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		var handler = CreateHandler(db);

		// Act & Assert
		await Assert.ThrowsAsync<CommentNotFoundException>(
			() => handler.Handle(new BanAuthorCommand(Guid.NewGuid()), CancellationToken.None));
	}

	private static BanAuthorHandler CreateHandler(DatabaseContext db) =>
		new(db, Logger<BanAuthorHandler>().Object);
}
