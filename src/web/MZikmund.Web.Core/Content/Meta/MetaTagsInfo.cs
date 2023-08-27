namespace MZikmund.Web.Core.Content.Meta;

public class MetaTagsInfo
{
	public string HtmlTitle
	{
		get
		{
			if (string.IsNullOrEmpty(Title))
			{
				return "Martin Zikmund";
			}
			else
			{
				return $"{Title} | Martin Zikmund";
			}
		}
	}

	public string MetaTitle => string.IsNullOrEmpty(Title) ? "Martin Zikmund" : Title;

	public string Title { get; set; } = "";

	public string Description { get; set; } =
		"Open-source enthusiast and Microsoft MVP. Passionate speaker, avid climber, and Lego aficionado.";

	public string Keywords { get; set; } =
		"software, open-source, development, IT, climbing, Lego";

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
