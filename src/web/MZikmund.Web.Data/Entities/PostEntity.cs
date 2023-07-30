using System.Diagnostics;

namespace MZikmund.Web.Data.Entities;

[DebuggerDisplay("{" + nameof(RouteName) + "}")]
public class PostEntity
{
	public PostEntity()
	{
		Categories = new HashSet<CategoryEntity>();
		Tags = new HashSet<TagEntity>();
	}

	public Guid Id { get; set; }

	public string Title { get; set; } = "";

	public DateTimeOffset? CreatedDate { get; set; }

	public DateTimeOffset? PublishedDate { get; set; }

	public DateTimeOffset? LastModifiedDate { get; set; }

	public PostStatus Status { get; set; }

	public string? HeroImageUrl { get; set; }

	public string LanguageCode { get; set; } = "";

	public string Content { get; set; } = "";

	public string Abstract { get; set; } = "";

	public string RouteName { get; set; } = "";

	public virtual ICollection<CategoryEntity> Categories { get; set; }

	public virtual ICollection<TagEntity> Tags { get; set; }
}
