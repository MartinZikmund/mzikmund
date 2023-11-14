namespace MZikmund.Web.Core.Services;

public interface IMarkdownConverter
{
	string ToHtml(string markdown);

	string ToPlainText(string markdown);
}
