// <copyright file="Index.cshtml.cs" company="Martin Zikmund">
// Copyright (c) Martin Zikmund. All rights reserved.
// </copyright>

using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Core.Blog;
using X.PagedList;

namespace MZikmund.Web.Pages.Blog;

public class IndexModel : PageModel
{
	private readonly IMediator _mediator;

	public IndexModel(IMediator mediator)
	{
		_mediator = mediator;
	}

	public StaticPagedList<PostListItem> BlogPosts { get; private set; } = null!;

	public async Task OnGet(int pageNumber = 1)
	{
		RouteData.Values.ToList();
		var pageSize = 12; // TODO: Include in configuration
						   //var pagesize = _blogConfig.ContentSettings.PostListPageSize;
		var posts = await _mediator.Send(new ListPostsQuery(pageNumber, pageSize));
		var totalPostsCount = await _mediator.Send(new CountPostsQuery());

		var list = new StaticPagedList<PostListItem>(posts, pageNumber, pageSize, totalPostsCount);

		BlogPosts = list;
	}
}
