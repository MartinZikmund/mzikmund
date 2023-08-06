using Microsoft.Extensions.Configuration;

namespace MZikmund.Web.Configuration.Connections;

public class ConnectionStringProvider : IConnectionStringProvider
{
	private const string DatabaseConnectionKey = "DatabaseConnection";
	private const string SendGridApiKey = "SendGridApiKey";
	private const string AzureBlobStorageKey = "AzureBlobStorage";

	private readonly IConfiguration _configuration;

	public ConnectionStringProvider(IConfiguration configuration)
	{
		_configuration = configuration;
	}

	public string? DatabaseConnection => _configuration.GetConnectionString(DatabaseConnectionKey);

	public string? SendGrid => _configuration.GetConnectionString(SendGridApiKey);

	public string? AzureBlobStorage => _configuration.GetConnectionString(AzureBlobStorageKey);
}
