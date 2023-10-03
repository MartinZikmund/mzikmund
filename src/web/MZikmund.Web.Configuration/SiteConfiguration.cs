using Microsoft.Extensions.Configuration;
using MZikmund.Web.Configuration.ConfigSections;

namespace MZikmund.Web.Configuration;

public class SiteConfiguration : ISiteConfiguration
{
	private readonly IConfiguration _configuration;

	public SiteConfiguration(IConfiguration configuration)
	{
		_configuration = configuration;
		_configuration.GetRequiredSection(nameof(Author)).Bind(Author);
		_configuration.GetRequiredSection(nameof(General)).Bind(General);
		_configuration.GetRequiredSection(nameof(MetaWeblog)).Bind(MetaWeblog);
	}

	public GeneralOptions General { get; } = new();

	public AuthorOptions Author { get; } = new();

	public MetaWeblogOptions MetaWeblog { get; } = new();
}
