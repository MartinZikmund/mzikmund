using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MZikmund.Web.Core.Blog;
using MZikmund.Web.Core.Dtos;
using MZikmund.Web.Core.Services;
using MZikmund.Web.Services;

namespace MZikmund.Web.Pages.Blog;

public class PostModel : PageModel
{
	private readonly IHostEnvironment _host;
	private readonly IMediator _mediator;
	private readonly IPostContentProcessor _postContentProcessor;

	public PostModel(
		IHostEnvironment host,
		IMediator mediator,
		IPostContentProcessor postContentProcessor)
	{
		_host = host;
		_mediator = mediator;
		_postContentProcessor = postContentProcessor;
	}

	public Post BlogPost { get; set; } = null!;

	public string HtmlContent { get; set; } = "";

	public string MetaKeywords { get; set; } = "";

	public async Task OnGet(string routeName)
	{
		BlogPost = await _mediator.Send(new GetPostByRouteNameQuery(routeName));
		HtmlContent = await _postContentProcessor.ProcessAsync(BlogPost.Content);
		MetaKeywords = string.Join(", ", BlogPost.Tags.Select(t => t.DisplayName));
	}
}
