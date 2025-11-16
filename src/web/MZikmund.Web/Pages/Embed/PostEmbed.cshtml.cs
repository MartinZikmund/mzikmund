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

	public Post? BlogPost { get; set; }

	public string HtmlContent { get; set; } = "";

	public string MetaKeywords { get; set; } = "";

	public async Task<IActionResult> OnGet(Guid id)
	{
		try
		{
			// Set headers to allow iframe embedding from the WASM app
			Response.Headers.Append("X-Frame-Options", "ALLOW-FROM https://mzikmund.app");
			Response.Headers.Append("Content-Security-Policy", "frame-ancestors https://mzikmund.app https://*.mzikmund.app");

			BlogPost = await _mediator.Send(new GetPostByIdQuery(id));
			HtmlContent = await _postContentProcessor.ProcessAsync(BlogPost.Content);
			MetaKeywords = string.Join(", ", BlogPost.Tags.Select(t => t.DisplayName));

			return Page();
		}
		catch
		{
			return NotFound();
		}
	}
}
