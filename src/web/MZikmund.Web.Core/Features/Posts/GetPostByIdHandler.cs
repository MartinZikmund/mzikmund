using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Infrastructure;

namespace MZikmund.Web.Core.Features.Posts;

public class GetPostByIdHandler : IRequestHandler<GetPostByIdQuery, Post>
{
	private readonly IRepository<PostEntity> _postsRepository;
	private readonly IMapper _mapper;

	public GetPostByIdHandler(
		IRepository<PostEntity> postsRepository,
		IMapper mapper)
	{
		_postsRepository = postsRepository;
		_mapper = mapper;
	}

	public async Task<Post> Handle(GetPostByIdQuery request, CancellationToken cancellationToken)
	{
		var now = DateTimeOffset.UtcNow;
		var post = await _postsRepository.AsQueryable()
			.Include(nameof(PostEntity.Tags))
			.Include(nameof(PostEntity.Categories))
			.Where(p => p.Status == PostStatus.Published && p.PublishedDate != null && p.PublishedDate <= now)
			.SingleOrDefaultAsync(p => p.Id.Equals(request.Id));
		return _mapper.Map<Post>(post);
	}
}
