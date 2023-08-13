namespace MZikmund.Web.Core.Dtos.Blog;

public class Post
{
	public string RouteName { get; set; } = "";

	public string Title { get; set; } = "";

	public string Content { get; set; } = "";

	public DateTimeOffset? PublishedDate { get; set; }

	public DateTimeOffset? LastModifiedDate { get; set; }
}
