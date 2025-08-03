using MediatR;
using MZikmund.Web.Data.Entities;
using MZikmund.Web.Data.Infrastructure;

namespace MZikmund.Web.Core.Features.Tags;

public class GetTagDisplayNamesHandler : IRequestHandler<GetTagDisplayNamesQuery, IReadOnlyList<string>>
{
	private readonly IRepository<TagEntity> _tagRepository;

	public GetTagDisplayNamesHandler(IRepository<TagEntity> tagRepository)
	{
		_tagRepository = tagRepository;
	}

	public async Task<IReadOnlyList<string>> Handle(GetTagDisplayNamesQuery request, CancellationToken cancellationToken) =>
		await _tagRepository.SelectAsync(t => t.DisplayName);
}
