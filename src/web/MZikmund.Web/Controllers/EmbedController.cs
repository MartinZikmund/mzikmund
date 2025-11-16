using MediatR;
using Microsoft.AspNetCore.Mvc;
using MZikmund.Web.Core.Features.Posts;
using MZikmund.Web.Core.Services;

namespace MZikmund.Web.Controllers;

/// <summary>
/// Provides chromeless views of blog posts for embedding.
/// </summary>
[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("api/embed")]
public class EmbedController : ControllerBase
{
	private readonly IMediator _mediator;
	private readonly IPostContentProcessor _postContentProcessor;

	/// <summary>
	/// Initializes a new instance of the <see cref="EmbedController"/> class.
	/// </summary>
	/// <param name="mediator">Mediator.</param>
	/// <param name="postContentProcessor">Post content processor.</param>
	public EmbedController(IMediator mediator, IPostContentProcessor postContentProcessor)
	{
		_mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
		_postContentProcessor = postContentProcessor ?? throw new ArgumentNullException(nameof(postContentProcessor));
	}

	/// <summary>
	/// Gets a chromeless HTML view of a blog post by ID.
	/// </summary>
	/// <param name="id">The post ID.</param>
	/// <returns>HTML content for embedding.</returns>
	[HttpGet]
	[Route("post/{id}")]
	[Produces("text/html")]
	public async Task<IActionResult> PostById(Guid id)
	{
		var post = await _mediator.Send(new GetPostByIdQuery(id));
		var htmlContent = await _postContentProcessor.ProcessAsync(post.Content);
		
		var html = GenerateEmbedHtml(post.Title, htmlContent);
		return Content(html, "text/html");
	}

	/// <summary>
	/// Gets a chromeless HTML view of a blog post by route name.
	/// </summary>
	/// <param name="routeName">The post route name.</param>
	/// <returns>HTML content for embedding.</returns>
	[HttpGet]
	[Route("post/route/{routeName}")]
	[Produces("text/html")]
	public async Task<IActionResult> PostByRouteName(string routeName)
	{
		var post = await _mediator.Send(new GetPostByRouteNameQuery(routeName));
		var htmlContent = await _postContentProcessor.ProcessAsync(post.Content);
		
		var html = GenerateEmbedHtml(post.Title, htmlContent);
		return Content(html, "text/html");
	}

	private static string GenerateEmbedHtml(string title, string content)
	{
		return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <meta name=""robots"" content=""noindex, nofollow"">
    <title>{title}</title>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 800px;
            margin: 0 auto;
            padding: 20px;
            background-color: #ffffff;
        }}
        h1, h2, h3, h4, h5, h6 {{
            margin-top: 1.5em;
            margin-bottom: 0.5em;
            color: #2c3e50;
        }}
        h1 {{
            font-size: 2em;
            border-bottom: 2px solid #3498db;
            padding-bottom: 0.3em;
        }}
        h2 {{
            font-size: 1.5em;
            border-bottom: 1px solid #ecf0f1;
            padding-bottom: 0.3em;
        }}
        a {{
            color: #3498db;
            text-decoration: none;
        }}
        a:hover {{
            text-decoration: underline;
        }}
        img {{
            max-width: 100%;
            height: auto;
            display: block;
            margin: 1em auto;
        }}
        pre {{
            background-color: #f8f9fa;
            border: 1px solid #dee2e6;
            border-radius: 4px;
            padding: 1em;
            overflow-x: auto;
        }}
        code {{
            background-color: #f8f9fa;
            padding: 0.2em 0.4em;
            border-radius: 3px;
            font-family: 'Courier New', Courier, monospace;
        }}
        pre code {{
            background-color: transparent;
            padding: 0;
        }}
        blockquote {{
            border-left: 4px solid #3498db;
            padding-left: 1em;
            margin-left: 0;
            color: #555;
            font-style: italic;
        }}
        table {{
            border-collapse: collapse;
            width: 100%;
            margin: 1em 0;
        }}
        th, td {{
            border: 1px solid #dee2e6;
            padding: 0.75em;
            text-align: left;
        }}
        th {{
            background-color: #f8f9fa;
            font-weight: bold;
        }}
    </style>
</head>
<body>
    <article class=""post-content"">
        {content}
    </article>
</body>
</html>";
	}
}
