namespace MZikmund.Web.Data.Entities;

public class BlogPostEntity
{
	public BlogPostEntity()
	{
		Categories = new HashSet<BlogCategoryEntity>();
		Tags = new HashSet<BlogTagEntity>();
	}

	public Guid Id { get; set; }

	public string Title { get; set; }

	public DateTimeOffset? PublishedDate { get; set; }

	public DateTimeOffset? LastModifiedDate { get; set; }

	public bool IsPublished { get; set; }

	public string HeroImageUrl { get; set; }

	public string LanguageCode { get; set; }

	public string Content { get; set; }

	public string Abstract { get; set; }

	public string RouteName { get; set; }

	public virtual ICollection<BlogCategoryEntity> Categories { get; set; }

	public virtual ICollection<BlogTagEntity> Tags { get; set; }
}
