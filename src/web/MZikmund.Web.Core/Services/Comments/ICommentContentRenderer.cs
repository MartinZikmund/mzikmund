namespace MZikmund.Web.Core.Services.Comments;

/// <summary>
/// Renders user-authored comment markdown into sanitized, storage-ready HTML.
/// </summary>
public interface ICommentContentRenderer
{
	/// <summary>
	/// Renders comment markdown to sanitized HTML that is safe to emit with <c>@Html.Raw</c>.
	/// </summary>
	/// <param name="markdown">The raw markdown exactly as the author typed it.</param>
	/// <returns>Sanitized HTML, or an empty string when the input is null or whitespace.</returns>
	string RenderHtml(string markdown);
}
