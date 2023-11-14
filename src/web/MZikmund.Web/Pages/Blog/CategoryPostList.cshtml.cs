// <copyright file="Index.cshtml.cs" company="Martin Zikmund">
// Copyright (c) Martin Zikmund. All rights reserved.
// </copyright>

using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Core.Blog;
using MZikmund.DataContracts.Blog;
using X.PagedList;

namespace MZikmund.Web.Pages.Blog;

public class CategoryPostListModel : PageModel
{
	private readonly IMediator _mediator;

	public CategoryPostListModel(IMediator mediator)
	{
		_mediator = mediator;
	}

	public Category Category { get; private set; } = null!;

	public StaticPagedList<PostListItem> BlogPosts { get; private set; } = null!;

	public async Task<IActionResult> OnGet(string categoryName, int pageNumber = 1)
	{
		var category = await _mediator.Send(new GetCategoryByRouteNameQuery(categoryName));
		if (category is null)
		{
			return NotFound();
		}

		Category = category;

		var pageSize = 12; // TODO: Include in configuration
						   //var pagesize = _blogConfig.ContentSettings.PostListPageSize;
		var posts = await _mediator.Send(new ListPostsQuery(pageNumber, pageSize, category.Id));
		var totalPostsCount = await _mediator.Send(new CountPostsQuery(CategoryId: category.Id));

		var list = new StaticPagedList<PostListItem>(posts, pageNumber, pageSize, totalPostsCount);

		BlogPosts = list;

		return Page();
	}
}
