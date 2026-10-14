using MZikmund.DataContracts.Blog.Comments;
using MZikmund.Web.Data.Entities;

namespace MZikmund.Web.Core.Features.Comments;

internal static class ModerationProjection
{
	/// <summary>
	/// Projects a comment for the moderation surface, which has no per-caller state: no vote flag,
	/// no ownership and no inline replies. The text of hidden comments stays blanked, as in the public DTO.
	/// </summary>
	/// <param name="comment">Entity with its <see cref="CommentEntity.Author"/> loaded.</param>
	public static CommentDto ToDto(CommentEntity comment) =>
		CommentProjection.ToDto(comment, currentAuthorId: null, includeReplies: false);
}
