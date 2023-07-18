namespace MZikmund.Web.Data.Entities;

public class PostRevisionEntity
{
	public Guid Id { get; set; }

	public Guid PostId { get; set; }

	public DateTimeOffset CreatedDate { get; set; }

	public string Content { get; set; } = "";
}
