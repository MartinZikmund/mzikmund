// <copyright file="Index.cshtml.cs" company="Martin Zikmund">
// Copyright (c) Martin Zikmund. All rights reserved.
// </copyright>

using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Core.Blog;
using X.PagedList;

namespace MZikmund.Web.Pages.Blog;

public class TagPostListModel : PageModel
{
	private readonly IMediator _mediator;

	public TagPostListModel(IMediator mediator)
	{
		_mediator = mediator;
	}

	public Tag Tag { get; private set; } = null!;

	public StaticPagedList<PostListItem> BlogPosts { get; private set; } = null!;

	public async Task<IActionResult> OnGet(string tagName, int pageNumber = 1)
	{
		var tag = await _mediator.Send(new GetTagByRouteNameQuery(tagName));
		if (tag is null)
		{
			return NotFound();
		}

		Tag = tag;

		var pageSize = 12; // TODO: Include in configuration
						   //var pagesize = _blogConfig.ContentSettings.PostListPageSize;
		var posts = await _mediator.Send(new ListPostsQuery(pageNumber, pageSize, TagId: tag.Id));
		var totalPostsCount = await _mediator.Send(new CountPostsQuery(TagId: tag.Id));

		var list = new StaticPagedList<PostListItem>(posts, pageNumber, pageSize, totalPostsCount);

		BlogPosts = list;

		return Page();
	}
}
