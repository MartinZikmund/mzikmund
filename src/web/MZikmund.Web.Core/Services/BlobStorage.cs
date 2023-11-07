using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using MZikmund.Web.Configuration;
using MZikmund.Web.Configuration.Connections;

namespace MZikmund.Web.Core.Services;

public class BlobStorage : IBlobStorage
{
	private readonly ISiteConfiguration _siteConfiguration;
	private readonly IConnectionStringProvider _connectionStringProvider;
	private readonly BlobContainerClient _container;
	private readonly ILogger<BlobStorage> _logger;

	public BlobStorage(ISiteConfiguration siteConfiguration, IConnectionStringProvider connectionStringProvider, ILogger<BlobStorage> logger)
	{
		_siteConfiguration = siteConfiguration;
		_connectionStringProvider = connectionStringProvider;
		_logger = logger;

		_container = new(connectionStringProvider.AzureBlobStorage, siteConfiguration.BlobStorage.MediaContainerName);

		logger.LogInformation($"Created {nameof(BlobStorage)} for account {_container.AccountName} on container {_container.Name}");
	}

	public async Task<string> AddAsync(string fileName, byte[] imageBytes)
	{
		if (string.IsNullOrWhiteSpace(fileName))
		{
			throw new ArgumentNullException(nameof(fileName));
		}

		_logger.LogInformation($"Uploading {fileName} to Azure Blob Storage.");


		var blob = _container.GetBlobClient(fileName);

		await using var stream = new MemoryStream(imageBytes);
		var uploadedBlob = await blob.UploadAsync(stream);

		_logger.LogInformation($"Uploaded media file '{fileName}' to Azure Blob Storage, ETag '{uploadedBlob.Value.ETag}'");

		return fileName;
	}

	public async Task DeleteAsync(string fileName) => await _container.DeleteBlobIfExistsAsync(fileName);

	public async Task<BlobInfo?> GetAsync(string fileName)
	{
		var blobClient = _container.GetBlobClient(fileName);
		await using var memoryStream = new MemoryStream();
		var extension = Path.GetExtension(fileName);
		if (string.IsNullOrWhiteSpace(extension))
		{
			throw new ArgumentException("File extension is empty");
		}

		var existsTask = blobClient.ExistsAsync();
		var downloadTask = blobClient.DownloadToAsync(memoryStream);

		var exists = await existsTask;
		if (!exists)
		{
			_logger.LogWarning($"Blob {fileName} not exist.");

			// Can not throw FileNotFoundException,
			// because hackers may request a large number of 404 images
			// to flood .NET runtime with exceptions and take out the server
			return null;
		}

		await downloadTask;
		var data = memoryStream.ToArray();

		var fileType = extension.Replace(".", string.Empty);
		var imageInfo = new BlobInfo(fileName, data);

		return imageInfo;
	}
}
