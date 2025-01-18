using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using MZikmund.Web.Configuration;
using MZikmund.Web.Configuration.Connections;
using MZikmund.Web.Core.Services.Blobs;

namespace MZikmund.Web.Core.Services;

public class BlobStorage : IBlobStorage
{
	private readonly ISiteConfiguration _siteConfiguration;
	private readonly IConnectionStringProvider _connectionStringProvider;
	private readonly BlobContainerClient _mediaContainer;
	private readonly BlobContainerClient _filesContainer;
	private readonly ILogger<BlobStorage> _logger;

	public BlobStorage(ISiteConfiguration siteConfiguration, IConnectionStringProvider connectionStringProvider, ILogger<BlobStorage> logger)
	{
		_siteConfiguration = siteConfiguration;
		_connectionStringProvider = connectionStringProvider;
		_logger = logger;

		_mediaContainer = new(connectionStringProvider.AzureBlobStorage, siteConfiguration.BlobStorage.MediaContainerName);
		_filesContainer = new(connectionStringProvider.AzureBlobStorage, siteConfiguration.BlobStorage.FilesContainerName);
	}

	public async Task InitializeAsync()
	{
		await _mediaContainer.CreateIfNotExistsAsync();
		await _mediaContainer.SetAccessPolicyAsync(PublicAccessType.Blob);
		await _filesContainer.CreateIfNotExistsAsync();
		await _filesContainer.SetAccessPolicyAsync(PublicAccessType.Blob);
	}

	public async Task<string> AddAsync(BlobKind blobKind, string fileName, byte[] imageBytes)
	{
		await using var stream = new MemoryStream(imageBytes);
		return await AddAsync(blobKind, fileName, stream);
	}

	public async Task<string> AddAsync(BlobKind blobKind, string fileName, Stream stream)
	{
		if (string.IsNullOrWhiteSpace(fileName))
		{
			throw new ArgumentNullException(nameof(fileName));
		}

		_logger.LogInformation($"Uploading {fileName} to Azure Blob Storage.");

		var containerClient = GetBlobContainerClient(blobKind);
		var blob = containerClient.GetBlobClient(fileName);

		var uploadedBlob = await blob.UploadAsync(stream);

		_logger.LogInformation($"Uploaded blob '{fileName}' to Azure Blob Storage, ETag '{uploadedBlob.Value.ETag}'");

		return fileName;
	}

	public async Task DeleteAsync(BlobKind blobKind, string fileName)
	{
		var containerClient = GetBlobContainerClient(blobKind);
		await containerClient.DeleteBlobIfExistsAsync(fileName);
	}

	public async Task<BlobInfo?> GetAsync(BlobKind blobKind, string fileName)
	{
		var containerClient = GetBlobContainerClient(blobKind);
		var blobClient = containerClient.GetBlobClient(fileName);
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

	public Task<BlobInfo?[]> ListAsync(BlobKind blobKind, string container) => throw new NotImplementedException();

	private BlobContainerClient GetBlobContainerClient(BlobKind blobKind)
	{
		var containerClient = blobKind switch
		{
			BlobKind.Image => _mediaContainer,
			BlobKind.File => _filesContainer,
			_ => throw new ArgumentOutOfRangeException(nameof(blobKind), blobKind, null)
		};

		return containerClient;
	}
}
