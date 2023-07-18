using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Hosting;
using MZikmund.Dtos.Blog.Posts;
using MZikmund.Dtos.Blog.Tags;
using MZikmund.Logic.Abstractions;
using MZikmund.Logic.Abstractions.Localization;
using MZikmund.Logic.Abstractions.Markdown;
using MZikmund.Logic.Services.Blog;
using MZikmund.Shared.Enums.Blog;

namespace MZikmund.Web.Areas.Blog.Pages;

public class PostModel : PageModel
{
	private readonly IHostEnvironment _host;
	private readonly IMarkdown _markdown;
	private readonly ILocalizationInfo _localizationInfo;
	private readonly IBlogPostsService _blogPostsService;
	private readonly IBlogTagsService _blogTagsService;
	private readonly IHighlightableGistHtmlGenerator _highlightableGistHtmlGenerator;

	public PostModel(
		IHostEnvironment host,
		IBlogPostsService blogPostsService,
		IBlogTagsService blogTagsService,
		IMarkdown markdown,
		ILocalizationInfo localizationInfo,
		IHighlightableGistHtmlGenerator highlightableGistHtmlGenerator)
	{
		_host = host;
		_blogPostsService = blogPostsService;
		_blogTagsService = blogTagsService;
		_markdown = markdown;
		_localizationInfo = localizationInfo;
		_highlightableGistHtmlGenerator = highlightableGistHtmlGenerator;
	}

	public BlogPostDto BlogPost { get; set; }

	public BlogTagDto[] Tags { get; set; }

	public async Task OnGet(int id)
	{
		BlogPost = await _blogPostsService.GetAsync(id);
		throw new NotImplementedException();
		//Tags = await _blogTagsService.GetForPostAsync(id, _localizationInfo.CurrentLanguageId);
		if (BlogPost.ContentType == BlogPostContentType.ExtendedMarkdown)
		{
			BlogPost.Localizations[0].Content = _markdown.ToHtml(BlogPost.Localizations[0].Content);
		}
		else
		{

		}
		//var matches = Regex.Matches(BlogPost.Content, "https://gist.github.com/.*");
		//foreach (Match match in matches)
		//{
		//    var generated = _highlightableGistHtmlGenerator.Generate(new Uri(match.Value));
		//    BlogPost.Content = BlogPost.Content.Replace(match.Value, generated, StringComparison.OrdinalIgnoreCase);
		//}
	}
}
