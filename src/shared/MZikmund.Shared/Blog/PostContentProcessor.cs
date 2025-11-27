using System.Text.RegularExpressions;

namespace MZikmund.Web.Core.Services;

public class PostContentProcessor : IPostContentProcessor
{
	private readonly IMarkdownConverter _markdownConverter;

	public PostContentProcessor(
		IMarkdownConverter markdownConverter)
	{
		_markdownConverter = markdownConverter;
	}

	public Task<string> ProcessAsync(string postContent, int caretPosition = 0)
	{
		var content = ReplaceGistUrlsWithEmbedScripts(postContent);
		
		// Inject an invisible marker at the caret position before converting to HTML
		if (caretPosition > 0 && caretPosition <= content.Length)
		{
			const string marker = "<div id=\"caret-marker\" style=\"display:none;\"></div>";
			content = content.Insert(caretPosition, marker);
		}
		
		content = _markdownConverter.ToHtml(content);
		return Task.FromResult(content);
	}

	public static string ReplaceGistUrlsWithEmbedScripts(string content)
	{
		// Pattern to match gist.github.com URLs
		string pattern = @"https://gist\.github\.com/[\w-]+/[\w-]+";

		// Use Regex to replace each matched URL with its embed script
		return Regex.Replace(content, pattern, match =>
		{
			return $"\n<script src=\"{match.Value}.js\"></script>\n";
		});
	}
}
