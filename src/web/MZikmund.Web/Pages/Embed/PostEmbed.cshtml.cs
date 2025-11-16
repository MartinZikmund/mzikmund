using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Core.Features.Posts;
using MZikmund.Web.Core.Services;

namespace MZikmund.Web.Pages.Embed;

public class PostEmbedModel : PageModel
{
	private readonly IMediator _mediator;
	private readonly IPostContentProcessor _postContentProcessor;

	public PostEmbedModel(IMediator mediator, IPostContentProcessor postContentProcessor)
	{
		_mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
		_postContentProcessor = postContentProcessor ?? throw new ArgumentNullException(nameof(postContentProcessor));
	}

	public Post? Post { get; set; }

	public string HtmlContent { get; set; } = "";

	public async Task<IActionResult> OnGet(Guid id)
	{
		try
		{
			Post = await _mediator.Send(new GetPostByIdQuery(id));
			HtmlContent = await _postContentProcessor.ProcessAsync(Post.Content);
			return Page();
		}
		catch
		{
			return NotFound();
		}
	}
}
