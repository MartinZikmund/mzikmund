using MediatR;
using Microsoft.AspNetCore.Http;

namespace MZikmund.Web.Core.Syndication;

public class GetRssHandler : IRequestHandler<GetRssQuery, string?>
{
	private readonly ISyndicationDataSource _syndicationDataSource;
	private readonly IFeedGenerator _feedGenerator;

	public GetRssHandler(
		ISyndicationDataSource syndicationDataSource,
		IHttpContextAccessor httpContextAccessor,
		IFeedGenerator feedGenerator)
	{
		_syndicationDataSource = syndicationDataSource;
		_feedGenerator = feedGenerator;
	}

	public async Task<string?> Handle(GetRssQuery request, CancellationToken ct)
	{
		var data = await _syndicationDataSource.GetFeedDataAsync(request.CategoryName, request.TagName);
		if (data is null)
		{
			return null;
		}

		return await _feedGenerator.GetRssAsync(data, request.CategoryName ?? "Default");
	}
}
