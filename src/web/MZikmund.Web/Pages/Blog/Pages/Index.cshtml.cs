// <copyright file="Index.cshtml.cs" company="Martin Zikmund">
// Copyright (c) Martin Zikmund. All rights reserved.
// </copyright>

using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MZikmund.Web.Areas.Blog.Pages;

public class IndexModel : PageModel
{
	private readonly IMediator _mediator;

	public IndexModel(IMediator mediator)
	{
		_mediator = mediator;
	}

	public BlogPostDto[] BlogPosts { get; set; }

	public async Task OnGet()
	{
		BlogPosts = (await _blogPostsService.GetAllAsync()).ToArray();
	}
}
