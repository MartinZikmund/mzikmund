using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MZikmund.Web.Core.Features.Categories;
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
			SiteTitle = $"Martin Zikmund — OPML",
			ContentInfo = categoryMap,
			BlogUrl = $"{rootUrl}/blog",
			RssUrl = $"{rootUrl}/rss",
			RssCategoryTemplate = $"{rootUrl}/rss/category/[catTitle]",
			BlogCategoryTemplate = $"{rootUrl}/blog/categories/[catTitle]"
		};

		var xml = await _mediator.Send(new GetOpmlQuery(opmlConfig));
		return Content(xml, "text/xml");
	}

	[HttpGet("rss")]
	public async Task<IActionResult> Rss() => await GenerateRssResponseAsync(new GetRssQuery(null, null));

	[HttpGet("rss/category/{categoryName}")]
	public async Task<IActionResult> RssByCategory([MaxLength(64)] string categoryName) =>
		await GenerateRssResponseAsync(new GetRssQuery(categoryName, null));

	[HttpGet("rss/tag/{tagName}")]
	public async Task<IActionResult> RssByTag([MaxLength(64)] string tagName) =>
		 await GenerateRssResponseAsync(new GetRssQuery(null, tagName));

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

	private async Task<IActionResult> GenerateRssResponseAsync(GetRssQuery query)
	{
		var xml = await _mediator.Send(query);
		if (string.IsNullOrWhiteSpace(xml))
		{
			return NotFound();
		}

		return Content(xml, "application/rss+xml; charset=utf-8");
	}
}
