namespace MZikmund.Models.Admin.Blog.Wordpress;

/// <summary>
/// Represents a tag imported from WordPress.
/// </summary>
public class WordpressTag
{
	/// <summary>
	/// Gets or sets the ID.
	/// </summary>
	public int Id { get; set; }

	/// <summary>
	/// Gets or sets the slug.
	/// </summary>
	public string Slug { get; set; } = "";

	/// <summary>
	/// Gets or sets the name.
	/// </summary>
	public string Name { get; set; } = "";
}
