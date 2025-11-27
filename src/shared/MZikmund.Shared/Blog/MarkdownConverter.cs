using ColorCode.Styling;
using Markdig;
using Markdig.Extensions.MediaLinks;
using Markdown.ColorCode;

namespace MZikmund.Web.Core.Services;

public class MarkdownConverter : IMarkdownConverter
{
	private readonly MarkdownPipeline _pipeline;

	public MarkdownConverter(bool includeSourceInformation)
	{
		_pipeline = new MarkdownPipelineBuilder()
			.UseAbbreviations()
			.UseAutoIdentifiers()
			.UseCitations()
			.UseCustomContainers()
			.UseDefinitionLists()
			.UseEmphasisExtras()
			.UseFigures()
			.UseFooters()
			.UseFootnotes()
			.UseGridTables()
			.UseMathematics()
			.UseMediaLinks()
			.UsePipeTables()
			.UseListExtras()
			.UseTaskLists()
			.UseDiagrams()
			.UseAutoLinks()
			.UseGenericAttributes()
			.UseBootstrap()
			.UseColorCode(HtmlFormatterType.Css)
			.Build();
	}

	public string ToHtml(string markdown) => Markdig.Markdown.ToHtml(markdown, _pipeline);

	public string ToPlainText(string markdown) => Markdig.Markdown.ToPlainText(markdown, _pipeline);
}
