using MediatR;
using Microsoft.EntityFrameworkCore;
using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Infrastructure;

namespace MZikmund.Web.Core.Features.Posts;

/// <summary>
/// Handler for counting all posts without filtering by published status (for admin use).
/// </summary>
public sealed class CountAllPostsHandler : IRequestHandler<CountAllPostsQuery, int>
{
	private readonly IRepository<PostEntity> _postRepository;

	public CountAllPostsHandler(IRepository<PostEntity> postRepo)
	{
		_postRepository = postRepo;
	}

	public async Task<int> Handle(CountAllPostsQuery request, CancellationToken ct)
	{
		var posts = _postRepository.AsQueryable();

		if (request.CategoryId is not null)
		{
			posts = posts.Where(p => p.Categories.Any(c => c.Id == request.CategoryId));
		}

		if (request.TagId is not null)
		{
			posts = posts.Where(p => p.Tags.Any(t => t.Id == request.TagId));
		}

		return await posts.CountAsync(ct);
	}
}
