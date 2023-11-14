using AutoMapper;
using MediatR;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Infrastructure;
using MZikmund.Web.Data.Specifications;
using MZikmund.Web.Services;

namespace MZikmund.Web.Core.Blog;

internal sealed class ListPostsHandler : IRequestHandler<ListPostsQuery, IReadOnlyList<PostListItem>>
{
	private readonly IRepository<PostEntity> _postsRepository;
	private readonly IRepository<CategoryEntity> _categoriesRepository;
	private readonly IRepository<TagEntity> _tagsRepository;
	private readonly IMarkdownConverter _markdownConverter;
	private readonly IMapper _mapper;

	public ListPostsHandler(
		IRepository<PostEntity> postsRepository,
		IRepository<CategoryEntity> categoriesRepository,
		IRepository<TagEntity> tagsRepository,
		IMarkdownConverter markdownConverter,
		IMapper mapper)
	{
		_postsRepository = postsRepository;
		_categoriesRepository = categoriesRepository;
		_tagsRepository = tagsRepository;
		_markdownConverter = markdownConverter;
		_mapper = mapper;
	}

	public async Task<IReadOnlyList<PostListItem>> Handle(ListPostsQuery request, CancellationToken cancellationToken)
	{
		var specification = new ListPostsSpecification(request.Page, request.PageSize, request.CategoryId, request.TagId);
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

		foreach (var post in posts)
		{
			post.Abstract = _markdownConverter.ToHtml(post.Abstract);
		}

		return posts;
	}
}
