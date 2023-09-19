using MediatR;
using Microsoft.AspNetCore.Mvc;
using MZikmund.Syndication;
using MZikmund.Web.Core.Blog;
using MZikmund.Web.Core.Syndication;
using System.ComponentModel.DataAnnotations;

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
}
