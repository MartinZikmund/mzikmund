// <copyright file="Index.cshtml.cs" company="Martin Zikmund">
// Copyright (c) Martin Zikmund. All rights reserved.
// </copyright>

using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Core.Features.Posts;
using MZikmund.Web.Core.Services;
using X.PagedList;

namespace MZikmund.Web.Pages.Blog;

public class IndexModel : PageModel
{
	private readonly IMediator _mediator;
	private readonly IPostContentProcessor _postContentProcessor;

	public IndexModel(IMediator mediator, IPostContentProcessor postContentProcessor)
	{
		_mediator = mediator;
		_postContentProcessor = postContentProcessor;
	}

	public StaticPagedList<PostListItem> BlogPosts { get; private set; } = null!;

	public async Task OnGet(int pageNumber = 1)
	{
		RouteData.Values.ToList();
		var pageSize = 12; // TODO: Include in configuration
						   //var pagesize = _blogConfig.ContentSettings.PostListPageSize;
		var posts = await _mediator.Send(new GetPostsQuery(pageNumber, pageSize));
		foreach (var post in posts.Data)
		{
			post.Abstract = await _postContentProcessor.ProcessAsync(post.Abstract);
		}
		var list = new StaticPagedList<PostListItem>(posts.Data, pageNumber, pageSize, posts.TotalCount);

		BlogPosts = list;
	}
}
