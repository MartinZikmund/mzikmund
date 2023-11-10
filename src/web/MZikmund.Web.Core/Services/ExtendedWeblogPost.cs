using WilderMinds.MetaWeblog;

namespace MZikmund.Web.Core.Services;

internal class ExtendedWeblogPost : Post
{
	public List<CustomField> custom_fields = new List<CustomField>();
}
