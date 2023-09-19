namespace MZikmund.Syndication;

public record OpmlConfig
{
	public string BlogUrl { get; set; } = "";

	public string RssUrl { get; set; } = "";

	public string SiteTitle { get; set; } = "";

	public string RssCategoryTemplate { get; set; } = "";

	public string BlogCategoryTemplate { get; set; } = "";

	public IEnumerable<KeyValuePair<string, string>> ContentInfo { get; set; } = new List<KeyValuePair<string, string>>();
}
