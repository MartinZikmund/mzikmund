using MediatR;
using Microsoft.AspNetCore.Http;

namespace MZikmund.Web.Core.Syndication;

public class GetAtomHandler : IRequestHandler<GetAtomQuery, string?>
{
	private readonly ISyndicationDataSource _syndicationDataSource;
	private readonly IFeedGenerator _feedGenerator;

	public GetAtomHandler(
		ISyndicationDataSource syndicationDataSource,
		IHttpContextAccessor httpContextAccessor,
		IFeedGenerator feedGenerator)
	{
		_syndicationDataSource = syndicationDataSource;
		_feedGenerator = feedGenerator;
	}

	public async Task<string?> Handle(GetAtomQuery request, CancellationToken ct)
	{
		var data = await _syndicationDataSource.GetFeedDataAsync(request.CategoryName);
		if (data is null)
		{
			return null;
		}

		return await _feedGenerator.GetAtomAsync(data);
	}
}
