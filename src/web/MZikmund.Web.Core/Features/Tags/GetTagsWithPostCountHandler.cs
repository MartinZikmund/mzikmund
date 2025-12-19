using MediatR;
using Microsoft.EntityFrameworkCore;
using MZikmund.DataContracts.Blog;
using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Extensions;
using MZikmund.Web.Data.Infrastructure;

namespace MZikmund.Web.Core.Features.Tags;

public class GetTagsWithPostCountHandler : IRequestHandler<GetTagsWithPostCountQuery, IReadOnlyList<TagWithPostCount>>
{
	private readonly IRepository<TagEntity> _tagRepository;

	public GetTagsWithPostCountHandler(
		IRepository<TagEntity> tagRepository)
	{
		_tagRepository = tagRepository;
	}

	public async Task<IReadOnlyList<TagWithPostCount>> Handle(GetTagsWithPostCountQuery request, CancellationToken cancellationToken)
	{
		var now = DateTimeOffset.UtcNow;
		var tags = await _tagRepository.AsQueryable()
			.Select(t => new TagWithPostCount
			{
				Id = t.Id,
				DisplayName = t.DisplayName,
				Description = t.Description,
				RouteName = t.RouteName,
				PostCount = t.Posts.AsQueryable().Count(PostEntityExtensions.IsPublishedAndVisible(now))
			})
			.OrderBy(t => t.DisplayName)
			.ToListAsync(cancellationToken);

		return tags;
	}
}
