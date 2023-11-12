namespace MZikmund.DataContracts.Blog.Posts;

public class LegacyBlogPostDto
{
	/// <summary>
	/// Gets or sets the legacy route name.
	/// </summary>
	public string LegacyRouteName { get; set; } = "";

	/// <summary>
	/// Gets or sets the language ID.
	/// </summary>
	public int LanguageId { get; set; }

	/// <summary>
	/// Gets or sets the blog post ID.
	/// </summary>
	public int PostId { get; set; }
}
