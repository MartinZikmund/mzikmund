namespace MZikmund.DataContracts.Blog;

/// <summary>
/// Admin-specific post data contract that includes the preview token.
/// </summary>
public class PostAdmin : Post
{
	/// <summary>
	/// Preview token used for shareable preview URLs without authentication.
	/// Only exposed through admin endpoints.
	/// </summary>
	public Guid? PreviewToken { get; set; }
}
