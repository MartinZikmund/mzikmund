using MZikmund.Web.Configuration.ConfigSections;

namespace MZikmund.Web.Configuration;

public interface ISiteConfiguration
{
	GeneralOptions General { get; }

	AuthorOptions Author { get; }

	BlobStorageOptions BlobStorage { get; }
}

