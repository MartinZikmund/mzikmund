using System.Globalization;
using ColorCode.Styling;
using Markdig;
using Markdig.Extensions.MediaLinks;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdown.ColorCode;

namespace MZikmund.Web.Core.Services;

public class MarkdownConverter : IMarkdownConverter
{
	private readonly MarkdownPipeline _pipeline;

	public MarkdownConverter()
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

	public string ToHtmlWithSourceLines(string markdown)
	{
		var document = Markdig.Markdown.Parse(markdown, _pipeline);

		using StringWriter writer = new();
		HtmlRenderer renderer = new(writer);
		_pipeline.Setup(renderer);
		foreach (var block in document)
		{
			var line = block.Line.ToString(CultureInfo.InvariantCulture);
			if (block is CodeBlock)
			{
				// The ColorCode renderer ignores block attributes, so code gets an anchor wrapper instead.
				renderer.Write("<div data-line=\"").Write(line).Write("\">");
				renderer.Render(block);
				renderer.Write("</div>").WriteLine();
			}
			else
			{
				block.GetAttributes().AddProperty("data-line", line);
				renderer.Render(block);
			}
		}

		writer.Flush();
		return writer.ToString();
	}

	public string ToPlainText(string markdown) => Markdig.Markdown.ToPlainText(markdown, _pipeline);
}
