namespace MZikmund.Web.Core.Syndication;

public interface IFeedGenerator
{
	Task<string> GetRssAsync(IEnumerable<FeedEntry> feedEntries);

	Task<string> GetAtomAsync(IEnumerable<FeedEntry> feedEntries);
}
