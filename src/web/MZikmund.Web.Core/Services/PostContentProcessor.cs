using System;
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
		var content = TransformWordpressCaption(post.Content);
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
}
