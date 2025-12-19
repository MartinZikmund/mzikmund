using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Infrastructure;

namespace MZikmund.Web.Core.Features.Posts;

/// <summary>
/// Handler for getting a post by ID without filtering by published status (for admin use).
/// </summary>
public class GetPostByIdForAdminHandler : IRequestHandler<GetPostByIdForAdminQuery, PostAdmin>
{
	private readonly IRepository<PostEntity> _postsRepository;
	private readonly IMapper _mapper;

	public GetPostByIdForAdminHandler(
		IRepository<PostEntity> postsRepository,
		IMapper mapper)
	{
		_postsRepository = postsRepository;
		_mapper = mapper;
	}

	public async Task<PostAdmin> Handle(GetPostByIdForAdminQuery request, CancellationToken cancellationToken)
	{
		var post = await _postsRepository.AsQueryable()
			.Include(nameof(PostEntity.Tags))
			.Include(nameof(PostEntity.Categories))
			.SingleOrDefaultAsync(p => p.Id.Equals(request.Id), cancellationToken);
		return _mapper.Map<PostAdmin>(post);
	}
}
