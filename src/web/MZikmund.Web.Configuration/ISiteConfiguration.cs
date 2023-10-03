using MZikmund.Web.Configuration.ConfigSections;

namespace MZikmund.Web.Configuration;

public interface ISiteConfiguration
{
	AuthorOptions Author { get; }
}

