using MZikmund.DataContracts.Blog;

namespace MZikmund.Web.Pages.Blog;

public interface IPostPageModel
{
	Post BlogPost { get; set; }
	string HtmlContent { get; set; }
	string MetaKeywords { get; set; }
	string? SafeHeroImageUrl { get; set; }
}
