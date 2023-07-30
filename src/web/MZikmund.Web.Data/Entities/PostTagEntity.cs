namespace MZikmund.Web.Data.Entities;

public class PostTagEntity
{
	public Guid PostId { get; set; }

	public Guid TagId { get; set; }

	public virtual PostEntity? Post { get; set; }

	public virtual TagEntity? Tag { get; set; }
}
