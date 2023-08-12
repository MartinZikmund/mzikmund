using System;
using System.Globalization;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using MZikmund.Web.Core.Dtos.Blog;
using MZikmund.Web.Services;

namespace MZikmund.Web.Core.Services;
public class PostContentProcessor : IPostContentProcessor
{
	private readonly IMarkdownConverter _markdownConverter;

	public PostContentProcessor(
		IMarkdownConverter markdownConverter)
	{
		_markdownConverter = markdownConverter;
	}

	public Task<string> ProcessAsync(Post post)
	{
		var content = ReplaceGistUrlsWithEmbedScripts(post.Content);
		content = _markdownConverter.ToHtml(content);
		return Task.FromResult(content);
	}

	public static string ReplaceGistUrlsWithEmbedScripts(string content)
	{
		// Pattern to match gist.github.com URLs
		string pattern = @"https://gist\.github\.com/[\w-]+/[\w-]+";

		// Use Regex to replace each matched URL with its embed script
		return Regex.Replace(content, pattern, match =>
		{
			return $"\n<script src=\"{match.Value}.js\"></script>\n";
		});
	}
}
