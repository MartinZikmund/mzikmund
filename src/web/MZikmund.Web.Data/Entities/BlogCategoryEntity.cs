namespace MZikmund.Web.Data.Entities;

public class BlogCategoryEntity
{
	public BlogCategoryEntity()
	{
		Posts = new HashSet<BlogPostEntity>();
	}

	public Guid Id { get; set; }

	public string DisplayName { get; set; } = "";

	public string? Description { get; set; }

	public string? Icon { get; set; }

	public string RouteName { get; set; } = "";

	public virtual ICollection<BlogPostEntity> Posts { get; set; }
}
