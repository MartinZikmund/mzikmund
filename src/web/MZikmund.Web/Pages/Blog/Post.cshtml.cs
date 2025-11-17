using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Core.Features.Posts;
using MZikmund.Web.Core.Services;

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

	public string? SafeHeroImageUrl { get; set; }

	public async Task OnGet(string routeName)
	{
		BlogPost = await _mediator.Send(new GetPostByRouteNameQuery(routeName));
		HtmlContent = await _postContentProcessor.ProcessAsync(BlogPost.Content);
		MetaKeywords = string.Join(", ", BlogPost.Tags.Select(t => t.DisplayName));
		
		// Validate and sanitize hero image URL
		if (!string.IsNullOrWhiteSpace(BlogPost.HeroImageUrl))
		{
			// Only allow http:// and https:// URLs for security
			// Explicitly reject javascript:, data:, and other potentially dangerous schemes
			if (Uri.TryCreate(BlogPost.HeroImageUrl, UriKind.Absolute, out var uri) &&
				(uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
				uri.Scheme != "javascript" && uri.Scheme != "data")
			{
				SafeHeroImageUrl = uri.ToString();
			}
		}
	}
}
