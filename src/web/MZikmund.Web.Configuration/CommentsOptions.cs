namespace MZikmund.Web.Configuration;

/// <summary>
/// Feature gate for the comments system.
/// Loaded from appsettings.json [Comments] section.
/// </summary>
public class CommentsOptions
{
	/// <summary>
	/// Whether the public comment routes and the comment UI are served.
	/// Admin routes are deliberately not gated by this flag.
	/// </summary>
	public bool Enabled { get; set; }
}
