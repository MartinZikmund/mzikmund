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
		client.DefaultRequestHeaders.Add("User-Agent", "GistReplacer");
	}

	public Task<string> ProcessAsync(Post post)
	{
		var content = TransformWordpressCaption(post.Content);
		content = ReplaceGistUrlsWithEmbedScripts(content);
		content = _markdownConverter.ToHtml(content);
		return Task.FromResult(content);
	}

	private string TransformWordpressCaption(string content)
	{
		// Define the patterns
		string captionWithLinkPattern = @"\[caption id=""[^""]*"" align=""[^""]*"" width=""[^""]*""\]\[(?<imageLinkWithOuterLink>!\[.*?\]\(.*?\))\]\((?<outerLink>.*?)\) (?<captionContent>.*?)\[/caption\]";
		string captionWithoutLinkPattern = @"\[caption id=""[^""]*"" align=""[^""]*"" width=""[^""]*""\](?<imageLink>!\[.*?\]\(.*?\)) (?<captionContent>.*?)\[/caption\]";

		// Transformation logic for captions with links
		string TransformWithLink(Match match)
		{
			var imageLinkWithOuterLink = match.Groups["imageLinkWithOuterLink"].Value;
			var outerLink = match.Groups["outerLink"].Value;
			var captionContent = match.Groups["captionContent"].Value;

			return $"\n^^^\n[{imageLinkWithOuterLink.Trim()}]({outerLink.Trim()})\n^^^ {captionContent}\n";
		}

		// Transformation logic for captions without links
		string TransformWithoutLink(Match match)
		{
			var imageLink = match.Groups["imageLink"].Value;
			var captionContent = match.Groups["captionContent"].Value;

			return $"\n^^^\n{imageLink.Trim()}\n^^^ {captionContent}\n";
		}

		// Apply the transformations
		var transformedContent = Regex.Replace(content, captionWithLinkPattern, TransformWithLink);
		transformedContent = Regex.Replace(transformedContent, captionWithoutLinkPattern, TransformWithoutLink);

		return transformedContent;
	}

	private static readonly HttpClient client = new HttpClient();
	private const string GistPattern = @"https://gist.github.com/([\w-]+)/([\w-]+)";

	private static async Task<string?> FetchGistContentAsync(string url)
	{
		var match = Regex.Match(url, GistPattern);
		if (!match.Success) return null;

		var gistId = match.Groups[2].Value;
		var apiUrl = $"https://api.github.com/gists/{gistId}";

		var response = await client.GetAsync(apiUrl);
		if (!response.IsSuccessStatusCode) return null;

		var jsonResponse = await response.Content.ReadAsStringAsync();
		var gistData = JsonConvert.DeserializeObject<GistResponse>(jsonResponse);

		var contentBuilder = new System.Text.StringBuilder();
		foreach (var file in gistData!.Files!.Values)
		{
			contentBuilder.Append($"\n```{file!.Language!.ToLower(CultureInfo.InvariantCulture)}\n{file.Content}\n```\n");
		}

		return contentBuilder.ToString();
	}

	public class GistFile
	{
		public string? Filename { get; set; }
		public string? Type { get; set; }
		public string? Language { get; set; }
		public string? Content { get; set; }
	}

	public class GistResponse
	{
		public Dictionary<string, GistFile>? Files { get; set; }
	}

	private static async Task<string> ReplaceGistsAsync(string content)
	{
		var matches = Regex.Matches(content, GistPattern);

		foreach (Match match in matches)
		{
			var gistContent = await FetchGistContentAsync(match.Value);
			if (gistContent != null)
			{
				content = content.Replace(match.Value, gistContent);
			}
		}

		return content;
	}

	public static string ReplaceGistUrlsWithEmbedScripts(string content)
	{
		// Pattern to match gist.github.com URLs
		string pattern = @"https://gist\.github\.com/[\w-]+/[\w-]+";

		// Use Regex to replace each matched URL with its embed script
		return Regex.Replace(content, pattern, match =>
		{
			return $"<script src=\"{match.Value}.js\"></script>";
		});
	}
}
