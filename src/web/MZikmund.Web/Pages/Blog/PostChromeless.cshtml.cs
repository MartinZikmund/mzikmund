using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Core.Features.Posts;
using MZikmund.Web.Core.Services;

namespace MZikmund.Web.Pages.Blog;

public class PostChromelessModel : PageModel
{
	private readonly IMediator _mediator;
	private readonly IPostContentProcessor _postContentProcessor;

	public PostChromelessModel(
		IMediator mediator,
		IPostContentProcessor postContentProcessor)
	{
		_mediator = mediator;
		_postContentProcessor = postContentProcessor;
	}

	public Post BlogPost { get; set; } = null!;

	public string HtmlContent { get; set; } = "";

	public async Task OnGet(Guid id)
	{
		BlogPost = await _mediator.Send(new GetPostByIdQuery(id));
		HtmlContent = await _postContentProcessor.ProcessAsync(BlogPost.Content);
	}
}
