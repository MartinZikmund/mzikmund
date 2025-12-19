using MediatR;
using Microsoft.EntityFrameworkCore;
using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Extensions;
using MZikmund.Web.Data.Infrastructure;

namespace MZikmund.Web.Core.Features.Posts;

public sealed class CountPostsHandler : IRequestHandler<CountPostsQuery, int>
{
	private readonly IRepository<PostEntity> _postRepository;

	public CountPostsHandler(IRepository<PostEntity> postRepo)
	{
		_postRepository = postRepo;
	}

	public async Task<int> Handle(CountPostsQuery request, CancellationToken ct)
	{
		var posts = _postRepository.AsQueryable();

		posts = posts.Where(PostEntityExtensions.IsPublishedAndVisible(DateTimeOffset.UtcNow));

		if (request.CategoryId is not null)
		{
			posts = posts.Where(p => p.Categories.Any(c => c.Id == request.CategoryId));
		}

		if (request.TagId is not null)
		{
			posts = posts.Where(p => p.Tags.Any(t => t.Id == request.TagId));
		}

		if (!string.IsNullOrWhiteSpace(request.SearchTerm))
		{
			// EF Core translates Contains to database query, which is case-insensitive by default in SQL Server
			posts = posts.Where(p => p.Title.Contains(request.SearchTerm) || 
				p.Content.Contains(request.SearchTerm) || 
				p.Abstract.Contains(request.SearchTerm) ||
				p.Tags.Any(t => t.DisplayName.Contains(request.SearchTerm)) ||
				p.Categories.Any(c => c.DisplayName.Contains(request.SearchTerm)));
		}

		return await posts.CountAsync(ct);
	}
}
