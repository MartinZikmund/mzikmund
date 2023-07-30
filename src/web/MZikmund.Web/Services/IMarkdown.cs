namespace MZikmund.Web.Services;

public interface IMarkdownConverter
{
	Task<string> ToHtmlAsync(string markdown);
}
