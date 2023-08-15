using AutoMapper;
using MediatR;
using MZikmund.Web.Core.Dtos;
using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Infrastructure;
using MZikmund.Web.Data.Specifications;

namespace MZikmund.Web.Core.Blog;

internal sealed class ListPostsHandler : IRequestHandler<ListPostsQuery, IReadOnlyList<PostListItem>>
{
	private readonly IRepository<PostEntity> _postsRepository;
	private readonly IMapper _mapper;

	public ListPostsHandler(
		IRepository<PostEntity> postsRepository,
		IMapper mapper)
	{
		_postsRepository = postsRepository;
		_mapper = mapper;
	}

	public async Task<IReadOnlyList<PostListItem>> Handle(ListPostsQuery request, CancellationToken cancellationToken)
	{
		var specification = new ListPostsSpecification(request.Page, request.PageSize);
		return await _postsRepository.SelectAsync(specification, post => new PostListItem
		{
			Content = post.Content, //TODO: Get summary only
			LastModifiedDate = post.LastModifiedDate,
			PublishedDate = post.PublishedDate,
			RouteName = post.RouteName,
			Title = post.Title
		});
	}
}
