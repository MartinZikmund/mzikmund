using AutoMapper;
using MediatR;
using MZikmund.DataContracts;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Core.Services;
using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Infrastructure;
using MZikmund.Web.Data.Specifications.Posts;

namespace MZikmund.Web.Core.Features.Posts;

/// <summary>
/// Handler for getting all posts without filtering by published status (for admin use).
/// </summary>
internal sealed class GetAllPostsHandler : IRequestHandler<GetAllPostsQuery, PagedResponse<PostListItem>>
{
	private readonly IRepository<PostEntity> _postsRepository;
	private readonly IMediator _mediator;
	private readonly IMapper _mapper;

	public GetAllPostsHandler(
		IRepository<PostEntity> postsRepository,
		IMediator mediator,
		IMapper mapper)
	{
		_postsRepository = postsRepository;
		_mediator = mediator;
		_mapper = mapper;
	}

	public async Task<PagedResponse<PostListItem>> Handle(GetAllPostsQuery request, CancellationToken cancellationToken)
	{
		var specification = new GetAllPostsSpecification(request.Page, request.PageSize, request.CategoryId, request.TagId);
		var postCount = await _mediator.Send(new CountAllPostsQuery(request.CategoryId, request.TagId), cancellationToken);

		var posts = await _postsRepository.SelectAsync(specification, post => new PostListItem
		{
			Id = post.Id,
			LastModifiedDate = post.LastModifiedDate,
			PublishedDate = post.PublishedDate,
			RouteName = post.RouteName,
			Title = post.Title,
			Abstract = post.Abstract,
			HeroImageAlt = post.HeroImageAlt,
			HeroImageUrl = post.HeroImageUrl,
			Tags = _mapper.Map<Tag[]>(post.Tags),
			Categories = _mapper.Map<Category[]>(post.Categories),
		}, cancellationToken);

		return new(posts, request.Page, request.PageSize, postCount);
	}
}
