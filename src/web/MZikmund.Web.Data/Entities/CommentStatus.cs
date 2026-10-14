namespace MZikmund.Web.Data.Entities;

/// <summary>
/// Represents the visibility status of a comment.
/// </summary>
public enum CommentStatus
{
	Visible = 0,
	DeletedByAuthor = 1,
	HiddenByModerator = 2,
}
