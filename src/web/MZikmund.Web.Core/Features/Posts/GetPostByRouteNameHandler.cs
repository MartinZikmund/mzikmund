using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Infrastructure;

namespace MZikmund.Web.Core.Features.Posts;

public class GetPostByRouteNameHandler : IRequestHandler<GetPostByRouteNameQuery, Post>
{
	private readonly IRepository<PostEntity> _postsRepository;
	private readonly IMapper _mapper;

	public GetPostByRouteNameHandler(
		IRepository<PostEntity> postsRepository,
		IMapper mapper)
	{
		_postsRepository = postsRepository;
		_mapper = mapper;
	}

	public async Task<Post> Handle(GetPostByRouteNameQuery request, CancellationToken cancellationToken)
	{
		var now = DateTimeOffset.UtcNow;
		var post = await _postsRepository.AsQueryable()
			.Where(p => p.RouteName.Equals(request.RouteName) && p.Status == PostStatus.Published && p.PublishedDate != null && p.PublishedDate <= now)
			.Include(nameof(PostEntity.Tags))
			.Include(nameof(PostEntity.Categories))
			.FirstOrDefaultAsync();
		return _mapper.Map<Post>(post);
	}
}
