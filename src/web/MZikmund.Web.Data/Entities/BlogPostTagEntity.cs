namespace MZikmund.Web.Data.Entities;

public class BlogPostTagEntity
{
	public Guid PostId { get; set; }

	public Guid TagId { get; set; }

	public virtual BlogPostEntity Post { get; set; }

	public virtual BlogTagEntity Tag { get; set; }
}
