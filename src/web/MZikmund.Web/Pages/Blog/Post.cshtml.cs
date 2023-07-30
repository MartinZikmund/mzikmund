using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MZikmund.Web.Core.Blog;
using MZikmund.Web.Core.Dtos.Blog;
using MZikmund.Web.Services;

namespace MZikmund.Web.Pages.Blog;

public class PostModel : PageModel
{
	private readonly IHostEnvironment _host;
	private readonly IMediator _mediator;
	private readonly IMarkdownConverter _markdownConverter;

	public PostModel(
		IHostEnvironment host,
		IMediator mediator,
		IMarkdownConverter markdownConverter)
	{
		_host = host;
		_mediator = mediator;
		_markdownConverter = markdownConverter;
	}

	public Post? BlogPost { get; set; }

	public Tag[]? Tags { get; set; }

	public async Task OnGet(string routeName)
	{
		BlogPost = await _mediator.Send(new GetPostByRouteNameQuery(routeName));
		//Tags = await _blogTagsService.GetForPostAsync(id, _localizationInfo.CurrentLanguageId);
		//if (BlogPost.ContentType == BlogPostContentType.ExtendedMarkdown)
		//{
		//	BlogPost.Localizations[0].Content = _markdown.ToHtml(BlogPost.Localizations[0].Content);
		//}
		//else
		//{

		//}
		//var matches = Regex.Matches(BlogPost.Content, "https://gist.github.com/.*");
		//foreach (Match match in matches)
		//{
		//    var generated = _highlightableGistHtmlGenerator.Generate(new Uri(match.Value));
		//    BlogPost.Content = BlogPost.Content.Replace(match.Value, generated, StringComparison.OrdinalIgnoreCase);
		//}
	}
}
