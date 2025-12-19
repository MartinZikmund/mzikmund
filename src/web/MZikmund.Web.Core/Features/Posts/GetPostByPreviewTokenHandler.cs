using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Infrastructure;

namespace MZikmund.Web.Core.Features.Posts;

public class GetPostByPreviewTokenHandler : IRequestHandler<GetPostByPreviewTokenQuery, Post>
{
	private readonly IRepository<PostEntity> _postsRepository;
	private readonly IMapper _mapper;

	public GetPostByPreviewTokenHandler(
		IRepository<PostEntity> postsRepository,
		IMapper mapper)
	{
		_postsRepository = postsRepository;
		_mapper = mapper;
	}

	public async Task<Post> Handle(GetPostByPreviewTokenQuery request, CancellationToken cancellationToken)
	{
		var post = await _postsRepository.AsQueryable()
			.Where(p => p.PreviewToken == request.PreviewToken)
			.Include(nameof(PostEntity.Tags))
			.Include(nameof(PostEntity.Categories))
			.FirstOrDefaultAsync(cancellationToken);
		return _mapper.Map<Post>(post);
	}
}
