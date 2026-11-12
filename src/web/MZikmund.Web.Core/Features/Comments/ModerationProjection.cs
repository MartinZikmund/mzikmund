using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Web.Data.Entities;

namespace MZikmund.Web.Core.Features.Comments;

internal static class ModerationProjection
{
	/// <summary>
	/// Projects a comment for the moderation surface, which has no per-caller state: no vote flag,
	/// no ownership and no inline replies. Hidden comments keep their text so moderators can read them; author-deleted ones stay blank.
	/// The author id is included so the feed can ban.
	/// </summary>
	/// <param name="comment">Entity with its <see cref="CommentEntity.Author"/> loaded.</param>
	public static CommentModerationDto ToDto(CommentEntity comment) =>
		new(
			CommentProjection.ToDto(comment, currentAuthorId: null, includeReplies: false, includeModeratedContent: true),
			comment.AuthorId);
}
