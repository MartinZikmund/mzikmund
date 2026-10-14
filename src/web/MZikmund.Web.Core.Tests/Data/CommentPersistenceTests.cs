using Microsoft.EntityFrameworkCore;
using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Web.Core.Features.Posts;
using MZikmund.Web.Core.Tests.Infrastructure;
using MZikmund.Web.Data;
using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Infrastructure;

namespace MZikmund.Web.Core.Tests.Data;

public class CommentPersistenceTests
{
	private static readonly Guid PostId = new("11111111-1111-1111-1111-111111111111");
	private static readonly Guid AuthorId = new("22222222-2222-2222-2222-222222222222");
	private static readonly Guid CommentId = new("33333333-3333-3333-3333-333333333333");

	private static async Task SeedAsync(DatabaseContext context)
	{
		context.Post.Add(new PostEntity { Id = PostId, Title = "Post", RouteName = "post" });
		context.CommentAuthor.Add(new CommentAuthorEntity
		{
			Id = AuthorId,
			DisplayName = "Martin",
			Email = "martin@example.com",
			CreatedOn = DateTime.UtcNow,
			LastSeenOn = DateTime.UtcNow,
		});
		context.Comment.Add(new CommentEntity
		{
			Id = CommentId,
			PostId = PostId,
			AuthorId = AuthorId,
			Content = "Hi",
			ContentHtml = "<p>Hi</p>",
			CreatedOn = DateTime.UtcNow,
		});
		context.CommentVote.Add(new CommentVoteEntity { CommentId = CommentId, AuthorId = AuthorId, CreatedOn = DateTime.UtcNow });
		context.CommentReport.Add(new CommentReportEntity
		{
			Id = Guid.NewGuid(),
			CommentId = CommentId,
			ReporterId = AuthorId,
			Reason = CommentReportReason.Spam,
			CreatedOn = DateTime.UtcNow,
		});
		await context.SaveChangesAsync(CancellationToken.None);
	}

	[Fact]
	public async Task Read_CommentTimestamps_AreUtc()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		var local = new DateTime(2026, 8, 11, 12, 0, 0, DateTimeKind.Local);
		await using (var write = fixture.CreateContext())
		{
			await SeedAsync(write);
			var comment = await write.Comment.SingleAsync(CancellationToken.None);
			comment.CreatedOn = local;
			comment.EditedOn = local;
			await write.SaveChangesAsync(CancellationToken.None);
		}

		// Act
		await using var read = fixture.CreateContext();
		var loaded = await read.Comment.AsNoTracking().SingleAsync(CancellationToken.None);

		// Assert
		Assert.Equal(DateTimeKind.Utc, loaded.CreatedOn.Kind);
		Assert.Equal(DateTimeKind.Utc, loaded.EditedOn!.Value.Kind);
		Assert.Equal(local.ToUniversalTime(), loaded.CreatedOn);
	}

	[Fact]
	public async Task Handle_HardDeleteWithComments_RemovesTheCommentsAndThePost()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		await using var context = fixture.CreateContext();
		await SeedAsync(context);
		DeletePostHandler handler = new(new DbContextRepository<PostEntity>(context), context);

		// Act
		await handler.Handle(new DeletePostCommand(PostId, false), CancellationToken.None);

		// Assert
		await using var verify = fixture.CreateContext();
		Assert.Empty(await verify.Post.ToListAsync(CancellationToken.None));
		Assert.Empty(await verify.Comment.ToListAsync(CancellationToken.None));
		Assert.Empty(await verify.CommentVote.ToListAsync(CancellationToken.None));
		Assert.Empty(await verify.CommentReport.ToListAsync(CancellationToken.None));
		Assert.Single(await verify.CommentAuthor.ToListAsync(CancellationToken.None));
	}

	[Fact]
	public async Task Handle_SoftDeleteWithComments_KeepsTheComments()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		await using var context = fixture.CreateContext();
		await SeedAsync(context);
		DeletePostHandler handler = new(new DbContextRepository<PostEntity>(context), context);

		// Act
		await handler.Handle(new DeletePostCommand(PostId, true), CancellationToken.None);

		// Assert
		await using var verify = fixture.CreateContext();
		Assert.Single(await verify.Comment.ToListAsync(CancellationToken.None));
		Assert.Equal(PostStatus.Deleted, (await verify.Post.SingleAsync(CancellationToken.None)).Status);
	}
}
