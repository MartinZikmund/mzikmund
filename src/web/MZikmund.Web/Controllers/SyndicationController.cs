using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MZikmund.Web.Core.Blog;
using MZikmund.Web.Core.Syndication;

namespace MZikmund.Web.Controllers;

[ApiController]
public class SubscriptionController : ControllerBase
{
	private readonly IMediator _mediator;

	public SubscriptionController(
		IMediator mediator)
	{
		_mediator = mediator;
	}

	[HttpGet("opml")]
	public async Task<IActionResult> Opml()
	{
		var cats = await _mediator.Send(new GetCategoriesQuery());
		var categoryMap = cats.Select(c => new KeyValuePair<string, string>(c.DisplayName, c.RouteName));
		var rootUrl = HttpContext.Request.GetBaseUrl();

		var opmlConfig = new OpmlConfig
		{
			SiteTitle = $"Martin Zikmund  — OPML",
			ContentInfo = categoryMap,
			BlogUrl = $"{rootUrl}/blog",
			RssUrl = $"{rootUrl}/rss",
			RssCategoryTemplate = $"{rootUrl}/rss/[catTitle]",
			BlogCategoryTemplate = $"{rootUrl}/blog/categories/[catTitle]"
		};

		var xml = await _mediator.Send(new GetOpmlQuery(opmlConfig));
		return Content(xml, "text/xml");
	}

	[HttpGet("rss/{categoryName?}")]
	public async Task<IActionResult> Rss([MaxLength(64)] string? categoryName = null)
	{
		var xml = await _mediator.Send(new GetRssQuery(categoryName));
		if (string.IsNullOrWhiteSpace(xml))
		{
			return NotFound();
		}

		return Content(xml, "text/xml");
	}

	[HttpGet("atom/{categoryName?}")]
	public async Task<IActionResult> Atom([MaxLength(64)] string? categoryName = null)
	{
		var xml = await _mediator.Send(new GetAtomQuery(categoryName));
		if (string.IsNullOrWhiteSpace(xml))
		{
			return NotFound();
		}

		return Content(xml, "text/xml");
	}
}
