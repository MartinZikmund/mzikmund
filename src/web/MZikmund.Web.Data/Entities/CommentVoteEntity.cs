namespace MZikmund.Web.Data.Entities;

public class CommentVoteEntity
{
	public Guid CommentId { get; set; }

	public Guid AuthorId { get; set; }

	public DateTime CreatedOn { get; set; }
}
