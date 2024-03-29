﻿using ColorCode.Styling;
using Markdig;
using Markdown.ColorCode;

namespace MZikmund.Web.Core.Services;

public class MarkdownConverter : IMarkdownConverter
{
	private readonly MarkdownPipeline _pipeline;

	public MarkdownConverter()
	{
		_pipeline = new MarkdownPipelineBuilder()
			.UseAdvancedExtensions()
			.UseBootstrap()
			.UseColorCode(HtmlFormatterType.Css)
			.Build();
	}

	public string ToHtml(string markdown) => Markdig.Markdown.ToHtml(markdown, _pipeline);

	public string ToPlainText(string markdown) => Markdig.Markdown.ToPlainText(markdown, _pipeline);
}
