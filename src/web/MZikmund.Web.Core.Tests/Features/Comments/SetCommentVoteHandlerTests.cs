using Microsoft.EntityFrameworkCore;
using MZikmund.Web.Core.Features.Comments;
using MZikmund.Web.Core.Services.Comments;
using MZikmund.Web.Core.Tests.Infrastructure;
using MZikmund.Web.Data;
using MZikmund.Web.Data.Entities;
using static MZikmund.Web.Core.Tests.Features.Comments.CommentSeed;
using DataCommentStatus = MZikmund.Web.Data.Entities.CommentStatus;

namespace MZikmund.Web.Core.Tests.Features.Comments;

public class SetCommentVoteHandlerTests
{
	private readonly Guid _postId = Guid.NewGuid();
	private readonly Guid _voterId = Guid.NewGuid();
	private readonly Guid _commentAuthorId = Guid.NewGuid();

	[Fact]
	public async Task Handle_FirstUpvote_AddsVoteRowIncrementsCountAndFlagsTheDto()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		var commentId = await SeedCommentAsync(db, _commentAuthorId);
		var handler = CreateHandler(db);

		// Act
		var result = await handler.Handle(new SetCommentVoteCommand(commentId, true), CancellationToken.None);

		// Assert
		Assert.Equal(1, await db.CommentVote.CountAsync(v => v.CommentId == commentId, CancellationToken.None));
		var stored = await db.Comment.FirstAsync(c => c.Id == commentId, CancellationToken.None);
		Assert.Equal(1, stored.UpvoteCount);
		Assert.Equal(1, result.UpvoteCount);
		Assert.True(result.HasUpvoted);
	}

	[Fact]
	public async Task Handle_RepeatedUpvote_IsIdempotent()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		var commentId = await SeedCommentAsync(db, _commentAuthorId);
		var handler = CreateHandler(db);

		// Act
		await handler.Handle(new SetCommentVoteCommand(commentId, true), CancellationToken.None);
		var result = await handler.Handle(new SetCommentVoteCommand(commentId, true), CancellationToken.None);

		// Assert
		Assert.Equal(1, await db.CommentVote.CountAsync(v => v.CommentId == commentId, CancellationToken.None));
		var stored = await db.Comment.FirstAsync(c => c.Id == commentId, CancellationToken.None);
		Assert.Equal(1, stored.UpvoteCount);
		Assert.True(result.HasUpvoted);
	}

	[Fact]
	public async Task Handle_RemoveVote_DeletesRowAndDecrementsCount()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		var commentId = await SeedCommentAsync(db, _commentAuthorId);
		var handler = CreateHandler(db);
		await handler.Handle(new SetCommentVoteCommand(commentId, true), CancellationToken.None);

		// Act
		var result = await handler.Handle(new SetCommentVoteCommand(commentId, false), CancellationToken.None);

		// Assert
		Assert.Equal(0, await db.CommentVote.CountAsync(v => v.CommentId == commentId, CancellationToken.None));
		var stored = await db.Comment.FirstAsync(c => c.Id == commentId, CancellationToken.None);
		Assert.Equal(0, stored.UpvoteCount);
		Assert.False(result.HasUpvoted);
	}

	[Fact]
	public async Task Handle_RepeatedRemoveVote_IsIdempotent()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		var commentId = await SeedCommentAsync(db, _commentAuthorId);
		var handler = CreateHandler(db);

		// Act
		await handler.Handle(new SetCommentVoteCommand(commentId, false), CancellationToken.None);
		await handler.Handle(new SetCommentVoteCommand(commentId, false), CancellationToken.None);

		// Assert
		Assert.Equal(0, await db.CommentVote.CountAsync(v => v.CommentId == commentId, CancellationToken.None));
		var stored = await db.Comment.FirstAsync(c => c.Id == commentId, CancellationToken.None);
		Assert.Equal(0, stored.UpvoteCount);
	}

	[Fact]
	public async Task Handle_OwnComment_ThrowsAndWritesNoVote()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		var commentId = await SeedCommentAsync(db, _voterId);
		var handler = CreateHandler(db);

		// Act & Assert
		await Assert.ThrowsAsync<CommentConflictException>(
			() => handler.Handle(new SetCommentVoteCommand(commentId, true), CancellationToken.None));
		Assert.Equal(0, await db.CommentVote.CountAsync(CancellationToken.None));
		var stored = await db.Comment.FirstAsync(c => c.Id == commentId, CancellationToken.None);
		Assert.Equal(0, stored.UpvoteCount);
	}

	[Theory]
	[InlineData(DataCommentStatus.HiddenByModerator)]
	[InlineData(DataCommentStatus.DeletedByAuthor)]
	public async Task Handle_NonVisibleComment_ThrowsAndWritesNoVote(DataCommentStatus status)
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		var commentId = await SeedCommentAsync(db, _commentAuthorId, status);
		var handler = CreateHandler(db);

		// Act & Assert
		await Assert.ThrowsAsync<CommentConflictException>(
			() => handler.Handle(new SetCommentVoteCommand(commentId, true), CancellationToken.None));
		Assert.Equal(0, await db.CommentVote.CountAsync(CancellationToken.None));
	}

	[Fact]
	public async Task Handle_AnotherVoterCommitsBetweenCheckAndWrite_DoesNotLoseTheirIncrement()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var seedDb = fixture.CreateContext();
		var commentId = await SeedCommentAsync(seedDb, _commentAuthorId);
		var otherVoterId = Guid.NewGuid();
		AddAuthor(seedDb, otherVoterId, "Other");
		await seedDb.SaveChangesAsync(CancellationToken.None);

		using var competitor = await CreateWarmContextAsync(fixture);
		RaceInterceptor race = new("FROM \"CommentVote\"", async () =>
		{
			competitor.CommentVote.Add(new CommentVoteEntity { CommentId = commentId, AuthorId = otherVoterId, CreatedOn = Day(2) });
			await competitor.SaveChangesAsync(CancellationToken.None);
			await competitor.Comment
				.Where(c => c.Id == commentId)
				.ExecuteUpdateAsync(s => s.SetProperty(c => c.UpvoteCount, 1), CancellationToken.None);
		});
		using var db = fixture.CreateContext(race);
		var handler = CreateHandler(db);

		// Act
		var result = await handler.Handle(new SetCommentVoteCommand(commentId, true), CancellationToken.None);

		// Assert
		Assert.True(race.Fired);
		using var assertDb = fixture.CreateContext();
		Assert.Equal(2, await assertDb.CommentVote.CountAsync(v => v.CommentId == commentId, CancellationToken.None));
		var stored = await assertDb.Comment.FirstAsync(c => c.Id == commentId, CancellationToken.None);
		Assert.Equal(2, stored.UpvoteCount);
		Assert.Equal(2, result.UpvoteCount);
	}

	[Fact]
	public async Task Handle_SameUserVoteInsertedConcurrently_IsAnIdempotentNoOpInsteadOfAnError()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var seedDb = fixture.CreateContext();
		var commentId = await SeedCommentAsync(seedDb, _commentAuthorId);

		using var competitor = await CreateWarmContextAsync(fixture);
		RaceInterceptor race = new("FROM \"CommentVote\"", async () =>
		{
			competitor.CommentVote.Add(new CommentVoteEntity { CommentId = commentId, AuthorId = _voterId, CreatedOn = Day(2) });
			await competitor.SaveChangesAsync(CancellationToken.None);
			await competitor.Comment
				.Where(c => c.Id == commentId)
				.ExecuteUpdateAsync(s => s.SetProperty(c => c.UpvoteCount, 1), CancellationToken.None);
		});
		using var db = fixture.CreateContext(race);
		var handler = CreateHandler(db);

		// Act
		var result = await handler.Handle(new SetCommentVoteCommand(commentId, true), CancellationToken.None);

		// Assert
		Assert.True(race.Fired);
		using var assertDb = fixture.CreateContext();
		Assert.Equal(1, await assertDb.CommentVote.CountAsync(v => v.CommentId == commentId, CancellationToken.None));
		var stored = await assertDb.Comment.FirstAsync(c => c.Id == commentId, CancellationToken.None);
		Assert.Equal(1, stored.UpvoteCount);
		Assert.True(result.HasUpvoted);
		Assert.Equal(1, result.UpvoteCount);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Handle_CommentOnANonPublicPost_ThrowsNotFoundAndWritesNoVote(bool futureDated)
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		var commentId = await SeedCommentAsync(db, _commentAuthorId);
		if (futureDated)
		{
			await db.Post.ExecuteUpdateAsync(
				s => s.SetProperty(p => p.PublishedDate, (DateTimeOffset?)Now.AddDays(1)), CancellationToken.None);
		}
		else
		{
			await db.Post.ExecuteUpdateAsync(
				s => s.SetProperty(p => p.Status, PostStatus.Draft), CancellationToken.None);
		}
		var handler = CreateHandler(db);

		// Act & Assert
		await Assert.ThrowsAsync<CommentNotFoundException>(
			() => handler.Handle(new SetCommentVoteCommand(commentId, true), CancellationToken.None));
		Assert.Equal(0, await db.CommentVote.CountAsync(CancellationToken.None));
	}

	// A context first touched while another statement is open on the shared connection fails to
	// initialize, so the competing context is warmed up before the race starts.
	private static async Task<DatabaseContext> CreateWarmContextAsync(CommentsDbFixture fixture)
	{
		var context = fixture.CreateContext();
		await context.Comment.AnyAsync(CancellationToken.None);
		return context;
	}

	private async Task<Guid> SeedCommentAsync(
		DatabaseContext db,
		Guid commentAuthorId,
		DataCommentStatus status = DataCommentStatus.Visible)
	{
		AddPost(db, _postId);
		AddAuthor(db, _voterId, "Martin");
		if (commentAuthorId != _voterId)
		{
			AddAuthor(db, commentAuthorId, "Jane");
		}

		var comment = AddComment(db, _postId, commentAuthorId, status: status);
		await db.SaveChangesAsync(CancellationToken.None);
		return comment.Id;
	}

	private SetCommentVoteHandler CreateHandler(DatabaseContext dbContext) =>
		new(dbContext, CurrentUser(_voterId, "Martin").Object, FixedClock().Object, Logger<SetCommentVoteHandler>().Object);
}
