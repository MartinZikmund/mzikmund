using System.Globalization;
using System.IO;
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
			.UsePreciseSourceLocation()
			.Build();
	}

	public string ToHtml(string markdown)
	{
		var writer = new StringWriter();
		var renderer = new HtmlRenderer(writer);
		_pipeline.Setup(renderer);
		
		var document = Markdig.Markdown.Parse(markdown, _pipeline);
		
		// Add source line information to each block
		AddSourceLineAttributes(document);
		
		renderer.Render(document);
		writer.Flush();
		
		return writer.ToString();
	}

	private void AddSourceLineAttributes(MarkdownDocument document)
	{
		foreach (var block in document)
		{
			if (block.Line >= 0)
			{
				var attributes = block.TryGetAttributes();
				if (attributes is null)
				{
					attributes = new HtmlAttributes();
					block.SetAttributes(attributes);
				}
				attributes.AddProperty("data-source-line", block.Line.ToString(CultureInfo.InvariantCulture));
			}
		}
	}

	public string ToPlainText(string markdown) => Markdig.Markdown.ToPlainText(markdown, _pipeline);
}
