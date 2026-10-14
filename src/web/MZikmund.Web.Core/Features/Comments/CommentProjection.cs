using Microsoft.EntityFrameworkCore;
using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Web.Data.Entities;
using DataCommentStatus = MZikmund.Web.Data.Entities.CommentStatus;
using DtoCommentStatus = MZikmund.DataContracts.Blog.Comments.CommentStatus;

namespace MZikmund.Web.Core.Features.Comments;

/// <summary>
/// Shared projection turning comment rows into DTOs, including the per-caller flags AutoMapper
/// cannot produce and the bounded inline reply set.
/// </summary>
internal static class CommentProjection
{
	/// <summary>
	/// Number of replies embedded directly in a comment.
	/// </summary>
	public const int InlineReplyCount = 3;

	/// <summary>
	/// Restricts a comment query to what the public may read.
	/// </summary>
	public static IQueryable<CommentEntity> Visible(IQueryable<CommentEntity> comments) =>
		comments.Where(comment => comment.Status != DataCommentStatus.HiddenByModerator);

	/// <summary>
	/// Projects comments into flat rows, resolving the vote and ownership flags for the caller.
	/// </summary>
	public static IQueryable<CommentRow> SelectRows(IQueryable<CommentEntity> comments, Guid? currentAuthorId) =>
		comments.Select(comment => new CommentRow(
			comment.Id,
			comment.PostId,
			comment.ParentId,
			comment.Author.DisplayName,
			comment.Author.AvatarUrl,
			comment.MentionedDisplayName,
			comment.Content,
			comment.ContentHtml,
			comment.Status,
			comment.CreatedOn,
			comment.EditedOn,
			comment.UpvoteCount,
			comment.ReplyCount,
			currentAuthorId != null && comment.Votes.Any(vote => vote.AuthorId == currentAuthorId),
			currentAuthorId != null && comment.AuthorId == currentAuthorId));

	/// <summary>
	/// Loads the first <see cref="InlineReplyCount"/> visible replies of each parent, CreatedOn
	/// ascending. Every requested parent gets an entry, empty when it has no visible replies.
	/// </summary>
	public static async Task<Dictionary<Guid, CommentDto[]>> LoadInlineRepliesAsync(
		IQueryable<CommentEntity> comments,
		IReadOnlyList<Guid> parentIds,
		Guid? currentAuthorId,
		CancellationToken cancellationToken)
	{
		Dictionary<Guid, CommentDto[]> repliesByParentId = new();

		// One small query per parent: a correlated Take is not translatable on every provider.
		foreach (var parentId in parentIds)
		{
			var rows = await SelectRows(
				Visible(comments)
					.Where(comment => comment.ParentId == parentId)
					.OrderBy(comment => comment.CreatedOn)
					.Take(InlineReplyCount),
				currentAuthorId)
				.ToArrayAsync(cancellationToken);

			repliesByParentId[parentId] = rows.Select(row => ToDto(row)).ToArray();
		}

		return repliesByParentId;
	}

	/// <summary>
	/// Converts a row into its DTO, optionally carrying already-loaded replies. Bodies of comments
	/// that are not visible are blanked so a tombstone never leaks the text.
	/// </summary>
	public static CommentDto ToDto(CommentRow row, CommentDto[]? replies = null) => new()
	{
		Id = row.Id,
		PostId = row.PostId,
		ParentId = row.ParentId,
		AuthorDisplayName = row.AuthorDisplayName,
		AuthorAvatarUrl = row.AuthorAvatarUrl,
		MentionedDisplayName = row.MentionedDisplayName,
		Content = row.Status == DataCommentStatus.Visible ? row.Content : "",
		ContentHtml = row.Status == DataCommentStatus.Visible ? row.ContentHtml : "",
		Status = MapStatus(row.Status),
		CreatedOn = row.CreatedOn,
		EditedOn = row.EditedOn,
		UpvoteCount = row.UpvoteCount,
		ReplyCount = row.ReplyCount,
		HasUpvoted = row.HasUpvoted,
		IsOwn = row.IsOwn,
		Replies = replies ?? Array.Empty<CommentDto>(),
	};

	/// <summary>
	/// Converts a tracked entity into its DTO, resolving the caller's vote and ownership flags from
	/// the loaded navigations. Used by the write handlers, which already hold the entity.
	/// </summary>
	/// <param name="comment">Entity with its <see cref="CommentEntity.Author"/> navigation loaded.</param>
	/// <param name="currentAuthorId">Author the flags are resolved for, or <c>null</c> when anonymous.</param>
	/// <param name="includeReplies">When <c>false</c>, <see cref="CommentDto.Replies"/> is empty.</param>
	/// <param name="includeModeratedContent">
	/// When <c>true</c> (moderation surfaces only), the text of hidden comments is kept so moderators
	/// can read what they moderate. Author-deleted comments stay blank regardless.
	/// </param>
	public static CommentDto ToDto(
		CommentEntity comment,
		Guid? currentAuthorId,
		bool includeReplies,
		bool includeModeratedContent = false) => new()
		{
			Id = comment.Id,
			PostId = comment.PostId,
			ParentId = comment.ParentId,
			AuthorDisplayName = comment.Author.DisplayName,
			AuthorAvatarUrl = comment.Author.AvatarUrl,
			MentionedDisplayName = comment.MentionedDisplayName,
			Content = ShowsContent(comment.Status, includeModeratedContent) ? comment.Content : "",
			ContentHtml = ShowsContent(comment.Status, includeModeratedContent) ? comment.ContentHtml : "",
			Status = MapStatus(comment.Status),
			CreatedOn = comment.CreatedOn,
			EditedOn = comment.EditedOn,
			UpvoteCount = comment.UpvoteCount,
			ReplyCount = comment.ReplyCount,
			HasUpvoted = currentAuthorId is not null && comment.Votes.Any(vote => vote.AuthorId == currentAuthorId),
			IsOwn = currentAuthorId is not null && currentAuthorId == comment.AuthorId,
			Replies = includeReplies
			? comment.Replies
				.Where(reply => reply.Status != DataCommentStatus.HiddenByModerator)
				.OrderBy(reply => reply.CreatedOn)
				.Take(InlineReplyCount)
				.Select(reply => ToDto(reply, currentAuthorId, includeReplies: false))
				.ToArray()
			: Array.Empty<CommentDto>(),
		};

	private static bool ShowsContent(DataCommentStatus status, bool includeModeratedContent) =>
		status == DataCommentStatus.Visible
		|| (includeModeratedContent && status == DataCommentStatus.HiddenByModerator);

	private static DtoCommentStatus MapStatus(DataCommentStatus status) => status switch
	{
		DataCommentStatus.DeletedByAuthor => DtoCommentStatus.DeletedByAuthor,
		DataCommentStatus.HiddenByModerator => DtoCommentStatus.HiddenByModerator,
		_ => DtoCommentStatus.Visible,
	};
}
