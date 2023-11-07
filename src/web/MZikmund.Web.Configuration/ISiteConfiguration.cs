using MZikmund.Web.Configuration.ConfigSections;

namespace MZikmund.Web.Configuration;

public interface ISiteConfiguration
{
	GeneralOptions General { get; }

	AuthorOptions Author { get; }

	MetaWeblogOptions MetaWeblog { get; }

	BlobStorageOptions BlobStorage { get; }
}

