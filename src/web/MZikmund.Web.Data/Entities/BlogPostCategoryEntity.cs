namespace MZikmund.Web.Data.Entities;

public class BlogPostCategoryEntity
{
	public Guid PostId { get; set; }

	public Guid CategoryId { get; set; }

	public virtual BlogPostEntity Post { get; set; }

	public virtual BlogCategoryEntity Category { get; set; }
}
