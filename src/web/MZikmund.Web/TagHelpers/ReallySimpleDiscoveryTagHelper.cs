using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace MZikmund.Web.TagHelpers;

[HtmlTargetElement("rsd", TagStructure = TagStructure.NormalOrSelfClosing)]
public class ReallySimpleDiscoveryTagHelper : TagHelper
{
	public string Endpoint { get; set; } = "";

	public override void Process(TagHelperContext context, TagHelperOutput output)
	{
		output.TagName = "link";
		output.Attributes.SetAttribute("type", new HtmlString("application/rsd+xml"));
		output.Attributes.SetAttribute("rel", "EditURI");
		output.Attributes.SetAttribute("title", "RSD");
		output.Attributes.SetAttribute("href", Endpoint);
	}
}
