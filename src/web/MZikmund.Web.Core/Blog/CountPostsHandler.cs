using MediatR;
using Microsoft.EntityFrameworkCore;
using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Infrastructure;

namespace MZikmund.Web.Core.Blog;

public sealed class CountPostsHandler : IRequestHandler<CountPostsQuery, int>
{
	private readonly IRepository<PostEntity> _postRepository;

	public CountPostsHandler(IRepository<PostEntity> postRepo)
	{
		_postRepository = postRepo;
	}

	public async Task<int> Handle(CountPostsQuery request, CancellationToken ct)
	{
		IQueryable<PostEntity> posts = _postRepository.AsQueryable();

		posts = posts.Where(p => p.Status == PostStatus.Published);

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
