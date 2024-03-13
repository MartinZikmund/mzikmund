using AutoMapper;
using MediatR;
using MZikmund.DataContracts;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Core.Services;
using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Infrastructure;
using MZikmund.Web.Data.Specifications;

namespace MZikmund.Web.Core.Blog;

internal sealed class ListPostsHandler : IRequestHandler<ListPostsQuery, PagedResponse<PostListItem>>
{
	private readonly IRepository<PostEntity> _postsRepository;
	private readonly IRepository<CategoryEntity> _categoriesRepository;
	private readonly IRepository<TagEntity> _tagsRepository;
	private readonly IMarkdownConverter _markdownConverter;
	private readonly IMediator _mediator;
	private readonly IMapper _mapper;

	public ListPostsHandler(
		IRepository<PostEntity> postsRepository,
		IRepository<CategoryEntity> categoriesRepository,
		IRepository<TagEntity> tagsRepository,
		IMarkdownConverter markdownConverter,
		IMediator mediator,
		IMapper mapper)
	{
		_postsRepository = postsRepository;
		_categoriesRepository = categoriesRepository;
		_tagsRepository = tagsRepository;
		_markdownConverter = markdownConverter;
		_mediator = mediator;
		_mapper = mapper;
	}

	public async Task<PagedResponse<PostListItem>> Handle(ListPostsQuery request, CancellationToken cancellationToken)
	{
		var specification = new ListPostsSpecification(request.Page, request.PageSize, request.CategoryId, request.TagId);
		var postCount = await _mediator.Send(new CountPostsQuery(request.CategoryId, request.TagId), cancellationToken);

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
		});

		return new(posts, request.Page, request.PageSize, postCount);
	}
}
