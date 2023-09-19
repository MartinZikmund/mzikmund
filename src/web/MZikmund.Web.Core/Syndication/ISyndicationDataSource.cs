// Based on https://github.com/EdiWang/Moonglade/blob/cf5571b0db09c7722b310ca9922cdcd542e93a51/src/Moonglade.Syndication/SyndicationDataSource.cs

namespace MZikmund.Web.Core.Syndication;

public interface ISyndicationDataSource
{
	Task<IReadOnlyList<FeedEntry>?> GetFeedDataAsync(string? categoryRouteName = null);
}
