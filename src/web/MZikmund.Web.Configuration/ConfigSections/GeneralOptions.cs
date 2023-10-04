namespace MZikmund.Web.Configuration.ConfigSections;

public class GeneralOptions
{
	public string EngineName { get; set; } = "";

	public Uri EngineRepositoryUrl { get; set; } = null!;

	public string DefaultTitle { get; set; } = "";

	public string DefaultDescription { get; set; } = "";

	public string DefaultKeywords { get; set; } = "";

	public string TitleFormatString { get; set; } = "";

	public Uri Url { get; set; } = null!;
}
