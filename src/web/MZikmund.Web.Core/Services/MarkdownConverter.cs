using Markdig;

namespace MZikmund.Web.Services;

public class MarkdownConverter : IMarkdownConverter
{
	public string ToHtml(string markdown) => Markdown.ToHtml(markdown);

	public string ToPlainText(string markdown) => Markdown.ToPlainText(markdown);
}
