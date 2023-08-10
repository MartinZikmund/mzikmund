using Markdig;

namespace MZikmund.Web.Services;

public class MarkdownConverter : IMarkdownConverter
{
	public Task<string> ToHtmlAsync(string markdown)
	{
		return Task.FromResult(Markdown.ToHtml(markdown));
	}
}
