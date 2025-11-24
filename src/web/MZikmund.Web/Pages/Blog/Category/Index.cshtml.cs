// <copyright file="Index.cshtml.cs" company="Martin Zikmund">
// Copyright (c) Martin Zikmund. All rights reserved.
// </copyright>

using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Core.Features.Categories;

namespace MZikmund.Web.Pages.Blog.Category;

public class IndexModel : PageModel
{
	private readonly IMediator _mediator;

	public IndexModel(IMediator mediator)
	{
		_mediator = mediator;
	}

	public IReadOnlyList<CategoryWithPostCount> Categories { get; private set; } = Array.Empty<CategoryWithPostCount>();

	public async Task OnGet()
	{
		Categories = await _mediator.Send(new GetCategoriesWithPostCountQuery());
	}
}
