using System.Globalization;
using MZikmund.Web.Configuration;

namespace MZikmund.Web.Core.Content.Meta;

public class MetaTagsInfo
{
	private readonly ISiteConfiguration _siteConfiguration;

	public MetaTagsInfo(ISiteConfiguration siteConfiguration)
	{
		_siteConfiguration = siteConfiguration;
		Description = _siteConfiguration.General.DefaultDescription;
		Keywords = _siteConfiguration.General.DefaultKeywords;
	}

	public string HtmlTitle
	{
		get
		{
			if (string.IsNullOrEmpty(Title))
			{
				return _siteConfiguration.General.DefaultTitle;
			}
			else
			{
				return string.Format(
					CultureInfo.InvariantCulture,
					_siteConfiguration.General.TitleFormatString,
					Title);
			}
		}
	}

	public string MetaTitle => string.IsNullOrEmpty(Title) ?
		_siteConfiguration.General.DefaultTitle : Title;

	public string Title { get; set; } = "";

	public string Description { get; set; }

	public string Keywords { get; set; }

	public string Image { get; set; } = "https://cdn.mzikmund.dev/assets/socialbannermd.jpg";

	public string ImageAlt { get; set; } = "Author giving a talk at a tech conference.";

	public TwitterCardType TwitterCard { get; set; } = TwitterCardType.SummaryLargeImage;

	public string TwitterCardValue => TwitterCard switch
	{
		TwitterCardType.Summary => "summary",
		TwitterCardType.SummaryLargeImage => "summary_large_image",
		_ => throw new NotImplementedException("Unsupported Twitter card type")
	};
}
