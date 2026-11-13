using System.Text.RegularExpressions;

namespace MZikmund.Web.Core.Services;

public partial class PostContentProcessor : IPostContentProcessor
{
	private readonly IMarkdownConverter _markdownConverter;

	public PostContentProcessor(
		IMarkdownConverter markdownConverter)
	{
		_markdownConverter = markdownConverter;
	}

	public Task<string> ProcessAsync(string postContent)
	{
		var content = ReplaceGistUrlsWithEmbedScripts(postContent);
		content = _markdownConverter.ToHtml(content);
		return Task.FromResult(content);
	}

	public Task<string> ProcessPreviewAsync(string postContent)
	{
		var content = ReplaceGistUrlsWithPreviewFrames(postContent);
		content = _markdownConverter.ToHtmlWithSourceLines(content);
		return Task.FromResult(content);
	}

	public static string ReplaceGistUrlsWithEmbedScripts(string content)
	{
		// Use Regex to replace each matched URL with its embed script
		return GistUrlRegex().Replace(content, match =>
		{
			return $"\n<script src=\"{match.Value}.js\"></script>\n";
		});
	}

	// Gist embeds use document.write, which would wipe a live-updated page, so the preview isolates each in
	// a frame. No line breaks are added, so source lines below a gist stay accurate.
	public static string ReplaceGistUrlsWithPreviewFrames(string content) =>
		GistUrlRegex().Replace(content, match =>
			$"<iframe class=\"gist-preview\" title=\"Gist\" srcdoc=\"&lt;script src=&quot;{match.Value}.js&quot;&gt;&lt;/script&gt;\"></iframe>");

	[GeneratedRegex(@"https://gist\.github\.com/[\w-]+/[\w-]+")]
	private static partial Regex GistUrlRegex();
}
