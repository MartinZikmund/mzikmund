// <copyright file="Index.cshtml.cs" company="Martin Zikmund">
// Copyright (c) Martin Zikmund. All rights reserved.
// </copyright>

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MZikmund.Dtos.Blog.Posts;
using MZikmund.Logic.Services.Blog;

namespace MZikmund.Web.Areas.Blog.Pages;

public class IndexModel : PageModel
{
	private readonly IBlogPostsService _blogPostsService;

	public IndexModel(IBlogPostsService blogPostsService)
	{
		_blogPostsService = blogPostsService;
	}

	public BlogPostDto[] BlogPosts { get; set; }

	public async Task OnGet()
	{
		BlogPosts = (await _blogPostsService.GetAllAsync()).ToArray();
	}
}
