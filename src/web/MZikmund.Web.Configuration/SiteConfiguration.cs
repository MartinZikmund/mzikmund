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
	}

	public AuthorOptions Author { get; } = new();
}
