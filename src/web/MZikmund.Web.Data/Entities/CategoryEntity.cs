using System.Diagnostics;

namespace MZikmund.Web.Data.Entities;

[DebuggerDisplay("{" + nameof(DisplayName) + "} - {" + nameof(RouteName) + "}")]
public class CategoryEntity
{
	public CategoryEntity()
	{
		Posts = new HashSet<PostEntity>();
	}

	public Guid Id { get; set; }

	public string DisplayName { get; set; } = "";

	public string? Description { get; set; }

	public string? Icon { get; set; }

	public string RouteName { get; set; } = "";

	public virtual ICollection<PostEntity> Posts { get; set; }
}
