namespace MZikmund.Web.Data.Entities;

public class TagEntity
{
	public TagEntity()
	{
		Posts = new HashSet<PostEntity>();
	}

	public Guid Id { get; set; }

	public string DisplayName { get; set; } = "";

	public string? Description { get; set; }

	public string RouteName { get; set; } = "";

	public virtual ICollection<PostEntity> Posts { get; set; }
}
