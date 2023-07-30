// <copyright file="Index.cshtml.cs" company="Martin Zikmund">
// Copyright (c) Martin Zikmund. All rights reserved.
// </copyright>

using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MZikmund.Web.Core.Blog;
using MZikmund.Web.Core.Dtos.Blog;

namespace MZikmund.Web.Pages.Blog;

public class IndexModel : PageModel
{
	private readonly IMediator _mediator;

	public IndexModel(IMediator mediator)
	{
		_mediator = mediator;
	}

	public IReadOnlyList<Post> BlogPosts { get; set; } = Array.Empty<Post>();

	public async Task OnGet()
	{
		BlogPosts = await _mediator.Send(new GetPostsQuery());
	}
}
