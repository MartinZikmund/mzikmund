namespace MZikmund.Web.Core.Services;

public interface IPostContentProcessor
{
	Task<string> ProcessAsync(string postContent);

	/// <summary>
	/// HTML for the editor's live preview: blocks carry source lines and gists are sandboxed in frames,
	/// so the content can be swapped in place without reloading the page.
	/// </summary>
	Task<string> ProcessPreviewAsync(string postContent);
}
