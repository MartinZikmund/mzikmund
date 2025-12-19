namespace MZikmund.DataContracts.Blog;

/// <summary>
/// Admin-specific post data contract that includes the preview token.
/// </summary>
public class PostAdmin : Post
{
	// PreviewToken is already inherited from Post base class
	// This DTO is used only for admin endpoints to ensure PreviewToken is included
}
