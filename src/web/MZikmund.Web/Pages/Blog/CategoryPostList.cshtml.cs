// <copyright file="Index.cshtml.cs" company="Martin Zikmund">
// Copyright (c) Martin Zikmund. All rights reserved.
// </copyright>

using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Core.Features.Categories;
using MZikmund.Web.Core.Features.Posts;
using MZikmund.Web.Core.Services;
using X.PagedList;

namespace MZikmund.Web.Pages.Blog;

public class CategoryPostListModel : PageModel
{
	private readonly IMediator _mediator;
	private readonly IPostContentProcessor _postContentProcessor;

	public CategoryPostListModel(IMediator mediator, IPostContentProcessor postContentProcessor)
	{
		_mediator = mediator;
		_postContentProcessor = postContentProcessor;
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
		var posts = await _mediator.Send(new GetPostsQuery(pageNumber, pageSize, category.Id));
		foreach (var post in posts.Data)
		{
			post.Abstract = await _postContentProcessor.ProcessAsync(post.Abstract);
		}
		var list = new StaticPagedList<PostListItem>(posts.Data, pageNumber, pageSize, posts.TotalCount);

		BlogPosts = list;

		return Page();
	}
}
