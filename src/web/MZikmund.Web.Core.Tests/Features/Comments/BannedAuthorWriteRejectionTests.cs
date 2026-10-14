using MediatR;
using Microsoft.EntityFrameworkCore;
using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Web.Core.Features.Comments;
using MZikmund.Web.Core.Infrastructure;
using MZikmund.Web.Core.Services.Comments;
using MZikmund.Web.Core.Tests.Infrastructure;
using MZikmund.Web.Data;
using static MZikmund.Web.Core.Tests.Features.Comments.CommentSeed;

namespace MZikmund.Web.Core.Tests.Features.Comments;

/// <summary>
/// A ban set through <see cref="BanAuthorHandler"/> must stop every authored write at the pipeline.
/// </summary>
public class BannedAuthorWriteRejectionTests
{
	private readonly Guid _authorId = Guid.NewGuid();

	[Fact]
	public Task Handle_BannedAuthorCreatingAComment_IsRejected() =>
		AssertRejectedAsync<CreateCommentCommand, CommentDto>(new(Guid.NewGuid(), "spam", null));

	[Fact]
	public Task Handle_BannedAuthorEditingAComment_IsRejected() =>
		AssertRejectedAsync<UpdateCommentCommand, CommentDto>(new(Guid.NewGuid(), "edited spam"));

	[Fact]
	public Task Handle_BannedAuthorVoting_IsRejected() =>
		AssertRejectedAsync<SetCommentVoteCommand, CommentDto>(new(Guid.NewGuid(), true));

	[Fact]
	public async Task Handle_BannedAuthorDeletingAComment_IsLetThroughToTheHandler()
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		AddAuthor(db, _authorId, "Troublemaker");
		await db.SaveChangesAsync(CancellationToken.None);
		await new BanAuthorHandler(db, Logger<BanAuthorHandler>().Object)
			.Handle(new BanAuthorCommand(_authorId), CancellationToken.None);

		AuthoredRequestBehavior<DeleteCommentCommand, Unit> behavior = new(
			db,
			CurrentUser(_authorId, "Troublemaker").Object,
			FixedClock().Object,
			Logger<AuthoredRequestBehavior<DeleteCommentCommand, Unit>>().Object);
		var handlerReached = false;

		// Act
		await behavior.Handle(
			new DeleteCommentCommand(Guid.NewGuid()),
			_ =>
			{
				handlerReached = true;
				return Task.FromResult(Unit.Value);
			},
			CancellationToken.None);

		// Assert
		Assert.True(handlerReached);
		var stored = await db.CommentAuthor.AsNoTracking().FirstAsync(a => a.Id == _authorId, CancellationToken.None);
		Assert.True(stored.IsBanned);
	}

	[Fact]
	public Task Handle_BannedAuthorReportingAComment_IsRejected() =>
		AssertRejectedAsync<ReportCommentCommand, Unit>(new(Guid.NewGuid(), CommentReportReason.Spam, null));

	private async Task AssertRejectedAsync<TRequest, TResponse>(TRequest request)
		where TRequest : IAuthoredRequest
	{
		// Arrange
		using CommentsDbFixture fixture = new();
		using var db = fixture.CreateContext();
		AddAuthor(db, _authorId, "Troublemaker");
		await db.SaveChangesAsync(CancellationToken.None);
		await new BanAuthorHandler(db, Logger<BanAuthorHandler>().Object)
			.Handle(new BanAuthorCommand(_authorId), CancellationToken.None);

		AuthoredRequestBehavior<TRequest, TResponse> behavior = new(
			db,
			CurrentUser(_authorId, "Troublemaker").Object,
			FixedClock().Object,
			Logger<AuthoredRequestBehavior<TRequest, TResponse>>().Object);
		var handlerReached = false;

		// Act
		await Assert.ThrowsAsync<CommentForbiddenException>(
			() => behavior.Handle(
				request,
				_ =>
				{
					handlerReached = true;
					return Task.FromResult(default(TResponse)!);
				},
				CancellationToken.None));

		// Assert
		Assert.False(handlerReached);
		var stored = await db.CommentAuthor.FirstAsync(a => a.Id == _authorId, CancellationToken.None);
		Assert.True(stored.IsBanned);
	}
}
