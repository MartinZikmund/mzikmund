namespace MZikmund.Web.Core.Services;

public interface IMarkdownConverter
{
	string ToHtml(string markdown);

	/// <summary>
	/// Like <see cref="ToHtml"/>, but each top-level block carries its zero-based source line in <c>data-line</c>.
	/// </summary>
	string ToHtmlWithSourceLines(string markdown);

	string ToPlainText(string markdown);
}
