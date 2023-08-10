using AutoMapper;
using MediatR;
using MZikmund.Web.Core.Dtos.Blog;
using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Infrastructure;

namespace MZikmund.Web.Core.Blog;

internal sealed class GetPostsHandler : IRequestHandler<GetPostsQuery, IReadOnlyList<Post>>
{
	private readonly IRepository<PostEntity> _postsRepository;
	private readonly IMapper _mapper;

	public GetPostsHandler(
		IRepository<PostEntity> postsRepository,
		IMapper mapper)
	{
		_postsRepository = postsRepository;
		_mapper = mapper;
	}

	public async Task<IReadOnlyList<Post>> Handle(GetPostsQuery request, CancellationToken cancellationToken)
	{
		var posts = await _postsRepository.ListAsync();
		return posts.OrderByDescending(p => p.PublishedDate).Select(p => _mapper.Map<Post>(p)).ToArray();
	}
}
