using System.Diagnostics;

namespace MZikmund.Web.Data.Entities;

[DebuggerDisplay("{" + nameof(Id) + "}")]
public class CommentEntity
{
	public CommentEntity()
	{
		Replies = new HashSet<CommentEntity>();
		Votes = new HashSet<CommentVoteEntity>();
	}

	public Guid Id { get; set; }

	public Guid PostId { get; set; }

	public Guid? ParentId { get; set; }

	public Guid AuthorId { get; set; }

	/// <summary>
	/// Raw markdown exactly as the author typed it.
	/// </summary>
	public string Content { get; set; } = "";

	/// <summary>
	/// Rendered and sanitized HTML, produced on every write.
	/// </summary>
	public string ContentHtml { get; set; } = "";

	/// <summary>
	/// Display name of the comment a reply was actually aimed at, when the reply was re-pointed to the grandparent.
	/// </summary>
	public string? MentionedDisplayName { get; set; }

	public CommentStatus Status { get; set; }

	public DateTime CreatedOn { get; set; }

	public DateTime? EditedOn { get; set; }

	public int UpvoteCount { get; set; }

	public int ReplyCount { get; set; }

	public int ReportCount { get; set; }

	public virtual PostEntity Post { get; set; } = null!;

	public virtual CommentEntity? Parent { get; set; }

	public virtual CommentAuthorEntity Author { get; set; } = null!;

	public virtual ICollection<CommentEntity> Replies { get; set; }

	public virtual ICollection<CommentVoteEntity> Votes { get; set; }
}
