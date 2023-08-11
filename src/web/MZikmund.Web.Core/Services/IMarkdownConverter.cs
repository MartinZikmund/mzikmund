namespace MZikmund.Web.Services;

public interface IMarkdownConverter
{
	string ToHtml(string markdown);

	string ToPlainText(string markdown);
}
