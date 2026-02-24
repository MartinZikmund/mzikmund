using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Core.Services;
using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Infrastructure;

namespace MZikmund.Web.Core.Features.Posts;

public class GetPostByPreviewTokenHandler : IRequestHandler<GetPostByPreviewTokenQuery, Post>
{
	private readonly IRepository<PostEntity> _postsRepository;
	private readonly IMapper _mapper;
	private readonly IDateProvider _dateProvider;

	public GetPostByPreviewTokenHandler(
		IRepository<PostEntity> postsRepository,
		IMapper mapper,
		IDateProvider dateProvider)
	{
		_postsRepository = postsRepository;
		_mapper = mapper;
		_dateProvider = dateProvider;
	}

	public async Task<Post> Handle(GetPostByPreviewTokenQuery request, CancellationToken cancellationToken)
	{
		var now = _dateProvider.UtcNow;

		var post = await _postsRepository.AsQueryable()
			.Where(p => p.PreviewToken == request.PreviewToken)
			.Include(nameof(PostEntity.Tags))
			.Include(nameof(PostEntity.Categories))
			.FirstOrDefaultAsync(cancellationToken);

		if (post == null)
		{
			throw new InvalidOperationException($"Post with preview token {request.PreviewToken} not found.");
		}

		// Preview URLs are only available for:
		// 1. Draft posts (not published yet)
		// 2. Published posts with future publish date
		// Once a post is published and the publish date has passed, preview URL is disabled
		var isPublishedAndLive = post.Status == PostStatus.Published
			&& post.PublishedDate.HasValue
			&& post.PublishedDate.Value <= now;

		if (isPublishedAndLive)
		{
			throw new InvalidOperationException($"Preview is not available for published posts. Please use the regular post URL.");
		}

		return _mapper.Map<Post>(post);
	}
}
