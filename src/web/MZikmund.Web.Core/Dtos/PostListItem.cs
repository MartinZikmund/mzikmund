namespace MZikmund.Web.Core.Dtos;

public class PostListItem
{
	public string RouteName { get; set; } = "";

	public string Title { get; set; } = "";

	public string Content { get; set; } = "";

	public string Abstract { get; set; } = "";

	public DateTimeOffset? PublishedDate { get; set; }

	public DateTimeOffset? LastModifiedDate { get; set; }
}
