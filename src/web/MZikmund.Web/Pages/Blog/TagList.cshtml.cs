// <copyright file="Index.cshtml.cs" company="Martin Zikmund">
// Copyright (c) Martin Zikmund. All rights reserved.
// </copyright>

using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Core.Features.Tags;

namespace MZikmund.Web.Pages.Blog;

public class TagListModel : PageModel
{
	private readonly IMediator _mediator;

	public TagListModel(IMediator mediator)
	{
		_mediator = mediator;
	}

	public IReadOnlyList<TagWithPostCount> Tags { get; private set; } = Array.Empty<TagWithPostCount>();

	public async Task OnGet()
	{
		Tags = await _mediator.Send(new GetTagsWithPostCountQuery());
	}
}
